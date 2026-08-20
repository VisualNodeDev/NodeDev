using Blazor.Diagrams;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using NodeDev.Blazor.DiagramsModels;
using NodeDev.Blazor.NodeAttributes;
using NodeDev.Core;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Delegates;
using System.Numerics;

namespace NodeDev.Blazor.Components;

/// <summary>
/// Materializes the domain <see cref="Graph"/> as Blazor.Diagrams nodes, groups, ports, and links.
/// The domain graph remains authoritative; this class owns only the projection and the position
/// decorations that are needed to place projected models on the canvas.
/// </summary>
internal sealed class GraphDiagramProjection
{
	private readonly Graph Graph;
	private readonly BlazorDiagram Diagram;
	private readonly GraphDiagramSynchronizer Synchronizer;
	private readonly Action<BaseLinkModel, bool> ConfigureConnection;

	/// <summary>
	/// Creates a projection for one graph and one diagram. A projection is not reusable for another
	/// graph because all lookups and event handlers are tied to these instances.
	/// </summary>
	public GraphDiagramProjection(
		Graph graph,
		BlazorDiagram diagram,
		GraphDiagramSynchronizer synchronizer,
		Action<BaseLinkModel, bool> configureConnection)
	{
		Graph = graph;
		Diagram = diagram;
		Synchronizer = synchronizer;
		ConfigureConnection = configureConnection;
	}

	/// <summary>
	/// Finds the diagram model representing a domain node. Delegate nodes are represented by groups,
	/// while ordinary nodes are represented by <see cref="GraphNodeModel"/> instances.
	/// </summary>
	public NodeModel? FindNodeModel(Node node)
	{
		return (NodeModel?)Diagram.Nodes.OfType<GraphNodeModel>().FirstOrDefault(model => model.Node == node)
			?? Diagram.Groups.OfType<LambdaGroupModel>().FirstOrDefault(group => group.DelegateNode == node);
	}

	/// <summary>
	/// Finds the projected port for a domain connection, including ports rendered directly on lambda groups.
	/// </summary>
	public GraphPortModel? FindPort(Connection connection)
	{
		var nodePort = FindNodeModel(connection.Parent)?.Ports
			.OfType<GraphPortModel>()
			.FirstOrDefault(port => port.Connection == connection);
		if (nodePort != null)
			return nodePort;

		return Diagram.Groups
			.SelectMany(group => group.Ports)
			.OfType<GraphPortModel>()
			.FirstOrDefault(port => port.Connection == connection);
	}

	/// <summary>
	/// Finds the lambda group whose body owns the supplied callable scope.
	/// </summary>
	public LambdaGroupModel? FindLambdaGroup(string? bodyScopeId) => Diagram.Groups
		.OfType<LambdaGroupModel>()
		.FirstOrDefault(group => group.DelegateNode.BodyScopeId == bodyScopeId);

	/// <summary>
	/// Recreates the complete diagram projection. Canvas-originated callbacks are suppressed while
	/// models are cleared and restored so the rebuild is not mistaken for a user domain mutation.
	/// </summary>
	public void Rebuild()
	{
		Diagram.Batch(() =>
		{
			using var suppression = Synchronizer.SuppressCanvasMutations();
			Diagram.Links.Clear();
			Diagram.Groups.Clear();
			Diagram.Nodes.Clear();
			Initialize();
		});
	}

	/// <summary>
	/// Projects a newly added domain node using the specialized model required by its node type,
	/// then restores its parent lambda relationship.
	/// </summary>
	public void AddNode(Node node)
	{
		if (node is CreateDelegateNode delegateNode)
			AddLambdaGroupModel(delegateNode);
		else if (node is LambdaReturnNode { IsImplicit: true } boundaryReturn)
			AddBoundaryReturnToGroup(boundaryReturn);
		else
			AddGraphNodeModel(node);

		ReparentScopedModels();
	}

	/// <summary>
	/// Refreshes an existing projected node. Port topology is rebuilt only when the domain node's
	/// inputs or outputs changed; otherwise the cheaper model refresh path is used.
	/// </summary>
	public void Refresh(Node node)
	{
		if (node is CreateDelegateNode)
		{
			Rebuild();
			return;
		}
		if (node is LambdaReturnNode { IsImplicit: true } boundaryReturn)
		{
			FindLambdaGroup(boundaryReturn.CallableScopeId)?.Refresh();
			return;
		}

		var nodeModel = FindNodeModel(node) as GraphNodeModel;
		if (nodeModel == null)
			return;

		var oldPorts = nodeModel.Ports.ToList();
		var expectedPorts = node.InputsAndOutputs
			.Select(connection => (Connection: connection, IsInput: node.Inputs.Contains(connection)))
			.ToList();
		var portsAreUnchanged = oldPorts.Count == expectedPorts.Count && expectedPorts.All(expected =>
			oldPorts.OfType<GraphPortModel>().Any(port =>
				port.Connection == expected.Connection &&
				(port.Alignment == PortAlignment.Left) == expected.IsInput));

		if (portsAreUnchanged)
		{
			nodeModel.Refresh();
			return;
		}

		Diagram.Batch(() =>
		{
			using (Synchronizer.SuppressConnectionUpdates())
			{
				foreach (var link in oldPorts.SelectMany(port => port.Links).Distinct().ToList())
					Diagram.Links.Remove(link);

				foreach (var port in oldPorts)
					nodeModel.RemovePort(port);

				foreach (var expectedPort in expectedPorts)
					nodeModel.AddPort(new GraphPortModel(nodeModel, expectedPort.Connection, expectedPort.IsInput));
			}

			AddNodeLinks(node, onlyOutputs: false);
			nodeModel.Refresh();
		});
	}

	/// <summary>
	/// Builds the initial projection in dependency order: lambda groups first, ordinary nodes second,
	/// scope parenting third, and links last after every possible endpoint exists.
	/// </summary>
	public void Initialize()
	{
		foreach (var delegateNode in Graph.Nodes.Values.OfType<CreateDelegateNode>())
			AddLambdaGroupModel(delegateNode);

		foreach (var node in Graph.Nodes.Values.Where(node => node is not CreateDelegateNode and not LambdaReturnNode { IsImplicit: true }))
			AddGraphNodeModel(node);

		ReparentScopedModels();

		foreach (var node in Graph.Nodes.Values)
			AddNodeLinks(node, onlyOutputs: true);
	}

	/// <summary>
	/// Creates an ordinary diagram node and its ports, and wires position persistence back to the domain node.
	/// </summary>
	private GraphNodeModel AddGraphNodeModel(Node node)
	{
		EnsureInitialScopedPosition(node);
		var nodeModel = Diagram.Nodes.Add(new GraphNodeModel(node));
		foreach (var connection in node.InputsAndOutputs)
			nodeModel.AddPort(new GraphPortModel(nodeModel, connection, node.Inputs.Contains(connection)));

		nodeModel.Moved += GraphCanvas.OnNodeMoved;
		return nodeModel;
	}

	/// <summary>
	/// Gives a new scoped node a usable position inside its owning lambda without overwriting a saved position.
	/// </summary>
	private void EnsureInitialScopedPosition(Node node)
	{
		if (node.CallableScopeId == null || node.HasDecoration<NodeDecorationPosition>())
			return;

		var owner = Graph.GetOwningLambda(node.CallableScopeId);
		if (owner == null)
			return;

		var ownerPosition = owner.GetOrAddDecoration<NodeDecorationPosition>(() => new(Vector2.Zero)).Position;
		var groupPadding = FindLambdaGroup(owner.BodyScopeId)?.Padding ?? LambdaGroupModel.MinimumPadding;
		var existingNodesInScope = Diagram.Nodes
			.OfType<GraphNodeModel>()
			.Count(model => model.Node.CallableScopeId == node.CallableScopeId);
		var offset = new Vector2(groupPadding + existingNodesInScope * 220, groupPadding);
		node.AddDecoration(new NodeDecorationPosition(ownerPosition + offset));
	}

	/// <summary>
	/// Creates the group used to visualize a delegate body, including capture, delegate, and return-boundary ports.
	/// </summary>
	private LambdaGroupModel AddLambdaGroupModel(CreateDelegateNode node)
	{
		var group = Diagram.Groups.Add(new LambdaGroupModel(node));
		foreach (var capture in node.CaptureInputs)
			group.AddPort(new GraphPortModel(group, capture, true));
		group.AddPort(new GraphPortModel(group, node.DelegateOutput, false));
		if (group.BoundaryReturn is { } boundaryReturn)
			AddBoundaryReturnPorts(group, boundaryReturn);

		group.Moved += OnLambdaGroupMoved;
		return group;
	}

	/// <summary>
	/// Adds a newly created implicit lambda return node to the group that represents its callable scope.
	/// </summary>
	private void AddBoundaryReturnToGroup(LambdaReturnNode boundaryReturn)
	{
		var group = FindLambdaGroup(boundaryReturn.CallableScopeId);
		if (group == null)
			return;

		AddBoundaryReturnPorts(group, boundaryReturn);
		group.Refresh();
	}

	/// <summary>
	/// Adds any missing return-boundary ports while preserving ports that were already projected.
	/// </summary>
	private static void AddBoundaryReturnPorts(LambdaGroupModel group, LambdaReturnNode boundaryReturn)
	{
		foreach (var connection in boundaryReturn.Inputs)
		{
			if (group.Ports.OfType<GraphPortModel>().All(port => port.Connection != connection))
				group.AddPort(new GraphPortModel(group, connection, true));
		}
	}

	/// <summary>
	/// Reconciles diagram parent/child relationships with callable-scope ownership in the domain graph.
	/// This is performed after nodes and groups exist because nested lambdas may reference another group.
	/// </summary>
	private void ReparentScopedModels()
	{
		var groupsByScope = Diagram.Groups
			.OfType<LambdaGroupModel>()
			.ToDictionary(group => group.DelegateNode.BodyScopeId);

		foreach (var nodeModel in Diagram.Nodes.OfType<GraphNodeModel>())
			AttachToScope(nodeModel, nodeModel.Node.CallableScopeId, groupsByScope);

		foreach (var group in Diagram.Groups.OfType<LambdaGroupModel>())
			AttachToScope(group, group.DelegateNode.CallableScopeId, groupsByScope);

		foreach (var rootGroup in Diagram.Groups.OfType<LambdaGroupModel>().Where(group => group.Group == null))
			Diagram.SendToBack(rootGroup);
	}

	/// <summary>
	/// Moves a projected model between lambda groups when its desired scope differs from its current parent.
	/// </summary>
	private static void AttachToScope(NodeModel model, string? scopeId, IReadOnlyDictionary<string, LambdaGroupModel> groupsByScope)
	{
		groupsByScope.TryGetValue(scopeId ?? string.Empty, out var desiredGroup);
		if (model.Group == desiredGroup)
			return;

		model.Group?.RemoveChild(model);
		desiredGroup?.AddChild(model);
	}

	/// <summary>
	/// Projects links for a node and restores their saved bend vertices. During initial projection only
	/// outputs are visited so each domain connection is added once; refreshes may inspect both directions.
	/// </summary>
	private void AddNodeLinks(Node node, bool onlyOutputs)
	{
		var addedConnections = new HashSet<(string Source, string Target)>();
		foreach (var connection in onlyOutputs ? node.Outputs : node.InputsAndOutputs)
		{
			var portModel = FindPort(connection) ?? throw new InvalidOperationException($"No canvas port exists for {node.Name}.{connection.Name}.");
			foreach (var other in connection.Connections)
			{
				var connectionKey = connection.IsOutput ? (connection.Id, other.Id) : (other.Id, connection.Id);
				if (!addedConnections.Add(connectionKey))
					continue;

				var otherPortModel = FindPort(other) ?? throw new InvalidOperationException($"No canvas port exists for {other.Parent.Name}.{other.Name}.");
				var source = portModel;
				var target = otherPortModel;
				if (!onlyOutputs && node.Inputs.Contains(connection))
					(source, target) = (otherPortModel, portModel);

				LinkModel link;
				using (Synchronizer.SuppressConnectionUpdates())
					link = Diagram.Links.Add(new LinkModel(source, target));
				ConfigureConnection(link, true);

				var connectionWithVertices = GraphCanvas.GetConnectionContainingVertices(source.Connection, target.Connection);
				if (connectionWithVertices.Vertices.Count == 0)
					continue;

				Diagram.Batch(() =>
				{
					using var vertexLoading = Synchronizer.SuppressVertexLoading();
					foreach (var vertex in connectionWithVertices.Vertices)
						link.AddVertex(new(vertex.X, vertex.Y));
				});
			}
		}
	}

	/// <summary>
	/// Persists a moved lambda group's position and the absolute positions of all descendant nodes.
	/// </summary>
	private static void OnLambdaGroupMoved(MovableModel movableModel)
	{
		if (movableModel is not LambdaGroupModel group)
			return;

		var groupDecoration = group.DelegateNode.GetOrAddDecoration<NodeDecorationPosition>(() => new(Vector2.Zero));
		groupDecoration.Position = new((float)group.Position.X, (float)group.Position.Y);

		foreach (var nodeModel in group.GetDescendantNodeModels())
		{
			var decoration = nodeModel.Node.GetOrAddDecoration<NodeDecorationPosition>(() => new(Vector2.Zero));
			decoration.Position = new((float)nodeModel.Position.X, (float)nodeModel.Position.Y);
		}
	}
}
