using Blazor.Diagrams;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Behaviors;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Core.Routers;
using Blazor.Diagrams.Options;
using Microsoft.AspNetCore.Components;
using NodeDev.Blazor.DiagramsModels;
using NodeDev.Blazor.NodeAttributes;
using NodeDev.Blazor.Services;
using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.ManagerServices;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Delegates;
using NodeDev.Core.Types;
using System.Numerics;
using System.Reactive.Linq;

namespace NodeDev.Blazor.Components;

public partial class GraphCanvas : ComponentBase, IDisposable, IGraphCanvas
{
	[Parameter, EditorRequired]
	public Graph Graph { get; set; } = null!;

	[CascadingParameter]
	public Index IndexPage { get; set; } = null!;

	[Inject]
	internal DebuggedPathService DebuggedPathService { get; set; } = null!;

	private GraphManagerService GraphManagerService => Graph.Manager;

	private int PopupX = 0;
	private int PopupY = 0;
	private Vector2 PopupNodePosition;
	private Connection? PopupNodeConnection;
	private Node? PopupNode;
	private string? PopupCallableScopeId;

	private BlazorDiagram Diagram { get; set; } = null!;

	#region OnInitialized

	protected override void OnInitialized()
	{
		base.OnInitialized();
		_ = NodeProvider.WarmExtensionMethodCatalogAsync();

		Graph.GraphCanvas = this;

		var options = new BlazorDiagramOptions
		{
			GridSize = 30,
			AllowMultiSelection = true,
			Zoom =
			{
				Enabled = true,
				Inverse = true
			},
			Links =
			{
				DefaultRouter = new NormalRouter(),
				DefaultPathGenerator = new SmoothPathGeneratorWithDirectVertices()
			},
		};
		Diagram = new BlazorDiagram(options);
		Diagram.RegisterComponent<GraphNodeModel, GraphNodeWidget>();
		Diagram.RegisterComponent<LambdaGroupModel, LambdaGroupWidget>();
		Diagram.Options.Constraints.ShouldDeleteNode = ShouldDeleteNode;
		Diagram.Options.Constraints.ShouldDeleteGroup = ShouldDeleteGroup;
		Diagram.GetBehavior<KeyboardShortcutsBehavior>()?.SetShortcut("Delete", false, false, false, DeleteSelection);
		Diagram.KeyDown += Diagram_KeyDown;

		Diagram.Nodes.Removed += OnNodeRemoved;
		Diagram.Links.Added += x => OnConnectionAdded(x, false);
		Diagram.Links.Removed += OnConnectionRemoved;
		Diagram.SelectionChanged += SelectionChanged;
	}

	#endregion

	#region OnAfterRenderAsync

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (firstRender)
		{
			await Task.Delay(100);
			Diagram.Batch(InitializeCanvasWithGraphNodes);

			GraphChangedSubscription = Graph.SelfClass.Project.GraphChanged.Where(x => x.RequireUIRefresh && x.Graph == Graph).AcceptThenSample(TimeSpan.FromMilliseconds(250)).Subscribe(OnGraphChangedFromCore);
			NodeExecutingSubscription = Graph.SelfClass.Project.GraphNodeExecuting.Where(x => x.Executor.Graph == Graph).Buffer(TimeSpan.FromMilliseconds(250)).Subscribe(OnGraphNodeExecuting);
			NodeExecutedSubscription = Graph.SelfClass.Project.GraphNodeExecuted.Where(x => x.Executor.Graph == Graph).Sample(TimeSpan.FromMilliseconds(250)).Subscribe(OnGraphNodeExecuted);
		}
	}

	#endregion

	#region OnGraphNodeExecuting / OnGraphNodeExecuted

	private void OnGraphNodeExecuting(IList<(GraphExecutor Executor, Node Node, Connection Exec)> options)
	{
		InvokeAsync(() =>
		{
			foreach (var option in options.DistinctBy(x => x.Exec))
			{
				var nodeModel = Diagram.Nodes.OfType<GraphNodeModel>().FirstOrDefault(x => x.Node == option.Node);
				if (nodeModel == null)
					return;

				_ = nodeModel.OnNodeExecuting(option.Exec);
			}
		});
	}

	private void OnGraphNodeExecuted((GraphExecutor Executor, Node Node, Connection Exec) options)
	{
		InvokeAsync(() =>
		{
			var nodeModel = Diagram.Nodes.OfType<GraphNodeModel>().FirstOrDefault(x => x.Node == options.Node);
			if (nodeModel == null)
				return;

			nodeModel.OnNodeExecuted(options.Exec);
		});
	}

	#endregion

	#region OnGraphChangedFromCore / RefreshAll

	private void OnGraphChangedFromCore((Graph, bool) _)
	{
		InvokeAsync(() =>
		{
			UpdateNodes(); // update all the nodes

			StateHasChanged();
		});
	}

	#endregion

	#region UpdateConnectionType

	public void UpdatePortColor(Connection connection)
	{
		var port = FindPort(connection);
		if (port == null)
			return;

		var color = GetTypeShapeColor(connection.Type, connection.Parent.TypeFactory);
		foreach (var link in port.Links.Cast<LinkModel>())
			link.Color = color;

		Diagram.Refresh();
	}

	#endregion

	#region UpdateNodes

	private void UpdateNodes()
	{
		Diagram.Batch(() =>
		{
			DisableConnectionUpdate = true;
			DisableNodeRemovedUpdate = true;

			Diagram.Links.Clear();
			Diagram.Groups.Clear();
			Diagram.Nodes.Clear();

			InitializeCanvasWithGraphNodes();

			DisableNodeRemovedUpdate = false;
			DisableConnectionUpdate = false;
		});
	}

	private NodeModel? FindNodeModel(Node node)
	{
		return (NodeModel?)Diagram.Nodes.OfType<GraphNodeModel>().FirstOrDefault(x => x.Node == node)
			?? Diagram.Groups.OfType<LambdaGroupModel>().FirstOrDefault(x => x.DelegateNode == node);
	}

	private GraphPortModel? FindPort(Connection connection)
	{
		var nodePort = FindNodeModel(connection.Parent)?.Ports
			.OfType<GraphPortModel>()
			.FirstOrDefault(x => x.Connection == connection);
		if (nodePort != null)
			return nodePort;

		return Diagram.Groups
			.SelectMany(x => x.Ports)
			.OfType<GraphPortModel>()
			.FirstOrDefault(x => x.Connection == connection);
	}

	private LambdaGroupModel? FindLambdaGroup(string? bodyScopeId) => Diagram.Groups
		.OfType<LambdaGroupModel>()
		.FirstOrDefault(x => x.DelegateNode.BodyScopeId == bodyScopeId);

	#endregion

	#region Events from client

	private static ValueTask<bool> ShouldDeleteNode(NodeModel node)
	{
		return ValueTask.FromResult(node is not GraphNodeModel { Node: LambdaEntryNode });
	}

	private ValueTask<bool> ShouldDeleteGroup(GroupModel group)
	{
		if (group is not LambdaGroupModel lambdaGroup)
			return ValueTask.FromResult(true);

		GraphManagerService.RemoveNode(lambdaGroup.DelegateNode);
		return ValueTask.FromResult(false);
	}

	private async ValueTask DeleteSelection(global::Blazor.Diagrams.Core.Diagram diagram)
	{
		var selectedGroups = Diagram.Groups
			.OfType<LambdaGroupModel>()
			.Where(x => x.Selected)
			.ToHashSet();

		foreach (var group in selectedGroups.Where(x => !HasSelectedLambdaAncestor(x, selectedGroups)).ToArray())
			GraphManagerService.RemoveNode(group.DelegateNode);

		await KeyboardShortcutsDefaults.DeleteSelection(diagram);
	}

	private static bool HasSelectedLambdaAncestor(LambdaGroupModel group, HashSet<LambdaGroupModel> selectedGroups)
	{
		var parent = group.Group;
		while (parent != null)
		{
			if (parent is LambdaGroupModel lambdaParent && selectedGroups.Contains(lambdaParent))
				return true;
			parent = parent.Group;
		}

		return false;
	}

	#region Node Removed

	bool DisableNodeRemovedUpdate = false;

	public void OnNodeRemoved(NodeModel nodeModel)
	{
		if (DisableNodeRemovedUpdate)
			return;

		if (nodeModel is not GraphNodeModel graphNodeModel)
			return;

		var node = graphNodeModel.Node;

		foreach (var input in node.Inputs)
		{
			foreach (var connection in input.Connections.ToList())
				GraphManagerService.DisconnectConnectionBetween(input, connection);
		}

		foreach (var output in node.Outputs)
		{
			foreach (var connection in output.Connections.ToList())
				GraphManagerService.DisconnectConnectionBetween(output, connection);
		}

		GraphManagerService.RemoveNode(node);
	}

	#endregion

	#region Connection Added / Removed, Vertex Added / Removed

	private bool DisableConnectionUpdate = false;
	private void OnConnectionUpdated(BaseLinkModel baseLinkModel, Anchor old, Anchor newAnchor)
	{
		if (DisableConnectionUpdate || baseLinkModel.Source is PositionAnchor || baseLinkModel.Target is PositionAnchor)
			return;

		var source = ((GraphPortModel?)baseLinkModel.Source.Model);
		var destination = ((GraphPortModel?)baseLinkModel.Target.Model);

		if (source == null || destination == null)
			return;

		if (source.Alignment == PortAlignment.Left) // it's an input, let's swap it so the "source" is an output
		{
			DisableConnectionUpdate = true;
			var old2 = baseLinkModel.Source;
			baseLinkModel.SetSource(baseLinkModel.Target); // this is necessary as everything assumes that the source is an output and vice versa
			baseLinkModel.SetTarget(old2);
			DisableConnectionUpdate = false;

			(destination, source) = (source, destination);
		}

		GraphManagerService.AddNewConnectionBetweenOrCapture(source.Connection, destination.Connection);
	}

	/// <summary>
	/// This is called when the user starts dragging a connection. The link that is being dragged is not yet connected to the ports, the target will be a temporary PositionAnchor.
	/// This is also called during the initialization when creating the links from the graph itself. In that case 'force' is set to true to make sure the connection is created properly no matter what.
	/// </summary>
	public void OnConnectionAdded(BaseLinkModel baseLinkModel, bool force)
	{
		if (DisableConnectionUpdate && !force)
			return;

		baseLinkModel.SourceChanged += OnConnectionUpdated;
		baseLinkModel.TargetChanged += OnConnectionUpdated;
		baseLinkModel.TargetMarker = LinkMarker.Arrow;
		baseLinkModel.Segmentable = true;
		baseLinkModel.DoubleClickToSegment = true;
		baseLinkModel.VertexAdded += BaseLinkModel_VertexAdded;
		baseLinkModel.VertexRemoved += BaseLinkModel_VertexRemoved;

		if (baseLinkModel is LinkModel link)
		{
			if (link.Source.Model is GraphPortModel source)
			{
				link.Color = GetTypeShapeColor(source.Connection.Type, source.Connection.Parent.TypeFactory);
			}
		}
	}

	/// <summary>
	/// Return the output connection except for execs, in that case we return the input connection.
	/// This is because vertices are stored for the port, and execs conveniently only have one output connection while other types only have one input connection.
	/// </summary>
	/// <returns></returns>
	private static Connection GetConnectionContainingVertices(Connection source, Connection destination)
	{
		if (source.Type.IsExec) // execs can only have one connection, therefor they always contains the vertex information
			return source;
		else // if this is not an exec, the destination (input) will always contain the vertex information
			return destination;
	}

	private static void UpdateVerticesInConnection(Connection source, Connection destination, BaseLinkModel linkModel)
	{
		var connection = GetConnectionContainingVertices(source, destination);

		connection.UpdateVertices(linkModel.Vertices.Select(x => new Vector2((float)x.Position.X, (float)x.Position.Y)));

		var other = connection == source ? destination : source;
		other.UpdateVertices([]); // make sure there's no leftover vertices
	}

	private bool DisableVertexAddDuringLoading = false;
	private void BaseLinkModel_VertexRemoved(BaseLinkModel baseLinkModel, LinkVertexModel vertex)
	{
		if (baseLinkModel is LinkModel link && link.Source.Model is GraphPortModel source && link.Target.Model is GraphPortModel destination)
			UpdateVerticesInConnection(source.Connection, destination.Connection, link);
	}

	private void BaseLinkModel_VertexAdded(BaseLinkModel baseLinkModel, LinkVertexModel vertex)
	{
		if (baseLinkModel is LinkModel link && link.Source.Model is GraphPortModel source && link.Target.Model is GraphPortModel destination)
		{
			vertex.Moved += _ => Vertex_Moved(link);

			if (!DisableVertexAddDuringLoading)
				UpdateVerticesInConnection(source.Connection, destination.Connection, link);
		}
	}

	private static void Vertex_Moved(LinkModel link)
	{
		if (link.Source.Model is GraphPortModel source && link.Target.Model is GraphPortModel destination)
			UpdateVerticesInConnection(source.Connection, destination.Connection, link);
	}

	/// <summary>
	/// Event called from the UI when client deleted a connection between two nodes.
	/// This is also called when the user drops a connection onto the canvas, in that case the source or target will be a PositionAnchor.
	/// </summary>
	public void OnConnectionRemoved(BaseLinkModel baseLinkModel)
	{
		if (DisableConnectionUpdate)
			return;

		var source = ((GraphPortModel?)baseLinkModel.Source.Model)?.Connection;
		var destination = ((GraphPortModel?)baseLinkModel.Target.Model)?.Connection;

		if (source != null && destination != null)
		{
			GraphManagerService.DisconnectConnectionBetween(source, destination);

			UpdateVerticesInConnection(source, destination, baseLinkModel);
		}
		else
		{

			if (baseLinkModel.Source is PositionAnchor positionAnchor && destination != null)
				OnPortDroppedOnCanvas(destination, positionAnchor.GetPlainPosition()!);
			else if (baseLinkModel.Target is PositionAnchor positionAnchor2 && source != null)
				OnPortDroppedOnCanvas(source, positionAnchor2.GetPlainPosition()!);
		}
	}

	#endregion

	#region Node Moved

	public static void OnNodeMoved(MovableModel movableModel)
	{
		var node = ((GraphNodeModel)movableModel).Node;

		var decoration = node.GetOrAddDecoration<NodeDecorationPosition>(() => new(Vector2.Zero));
		decoration.Position = new((float)movableModel.Position.X, (float)movableModel.Position.Y);
	}

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

	#endregion

	#region OnPortDroppedOnCanvas

	private bool IsShowingNodeSelection = false;

	public void OnPortDroppedOnCanvas(Connection connection, global::Blazor.Diagrams.Core.Geometry.Point point)
	{
		PopupNode = connection.Parent;
		PopupNodeConnection = connection;
		PopupCallableScopeId = connection.Parent.CallableScopeId;
		var screenPosition = Diagram.GetScreenPoint(point.X, point.Y) - Diagram.Container!.NorthWest;
		PopupX = (int)screenPosition.X;
		PopupY = (int)screenPosition.Y;
		PopupNodePosition = new((float)point.X, (float)point.Y);
		IsShowingNodeSelection = true;

		StateHasChanged();
	}

	private void OnNewNodeTypeSelected(NodeProvider.NodeSearchResult searchResult)
	{
		var node = GraphManagerService.AddNode(searchResult, node =>
		{
			node.AddDecoration(new NodeDecorationPosition(new(PopupNodePosition.X, PopupNodePosition.Y)));
		}, PopupCallableScopeId);

		Diagram.Batch(() =>
		{
			if (PopupNodeConnection != null && PopupNode != null)
			{
				// check if the source was an input or output and choose the proper destination based on that
				List<Connection> sources, destinations;
				bool isPopupNodeInput = PopupNodeConnection.IsInput;
				if (isPopupNodeInput)
				{
					sources = PopupNode.Inputs;
					destinations = node.Outputs;
				}
				else
				{
					sources = PopupNode.Outputs;
					destinations = node.Inputs;
				}

				Connection? destination = null;
				if (PopupNodeConnection.Type is UndefinedGenericType) // can connect to anything except exec
					destination = destinations.FirstOrDefault(x => !x.Type.IsExec);
				else // can connect to anything that is assignable to the type
					destination = destinations.FirstOrDefault(x => PopupNodeConnection.Type.IsAssignableTo(x.Type, out _, out _) || (x.Type is UndefinedGenericType && !PopupNodeConnection.Type.IsExec));

				// if we found a connection, connect them together
				if (destination != null)
				{
					var source = isPopupNodeInput ? destination : PopupNodeConnection;
					var target = isPopupNodeInput ? PopupNodeConnection : destination;

					GraphManagerService.AddNewConnectionBetween(source, target);
				}
			}

			CancelPopup();
		});

	}

	#endregion

	#region OnOverloadSelectionRequested / OnNewOverloadSelected

	private bool IsShowingOverloadSelection = false;

	public void OnOverloadSelectionRequested(GraphNodeModel graphNode)
	{
		PopupNode = graphNode.Node;
		IsShowingOverloadSelection = true;

		StateHasChanged();
	}

	private void OnNewOverloadSelected(Node.AlternateOverload overload)
	{
		if (PopupNode == null)
			return;

		GraphManagerService.SelectNodeOverload(PopupNode, overload);

		// Refresh the node visually after overload selection
		// The node's ports have changed, so we need to update the UI
		Refresh(PopupNode);

		CancelPopup();
	}

	#endregion

	#region OnGenericTypeSelectionMenuAsked

	private bool IsShowingGenericTypeSelection = false;
	private string? GenericTypeSelectionMenuGeneric;
	private Action<TypeBase>? PopupTypeSelectedAction;

	public void OnGenericTypeSelectionMenuAsked(GraphNodeModel nodeModel, string undefinedGenericType)
	{
		PopupTypeSelectedAction = null;
		PopupNode = nodeModel.Node;
		var p = Diagram.GetScreenPoint(nodeModel.Position.X, nodeModel.Position.Y) - Diagram.Container!.NorthWest;
		PopupX = (int)p.X;
		PopupY = (int)p.Y;
		GenericTypeSelectionMenuGeneric = undefinedGenericType;
		IsShowingGenericTypeSelection = true;

		StateHasChanged();
	}

	private void OnGenericTypeSelected(TypeBase type)
	{
		if (PopupTypeSelectedAction != null)
		{
			PopupTypeSelectedAction(type);
			CancelPopup();
			return;
		}

		if (PopupNode == null || GenericTypeSelectionMenuGeneric == null)
			return;

		GraphManagerService.PropagateNewGeneric(PopupNode, new Dictionary<string, TypeBase>() { [GenericTypeSelectionMenuGeneric] = type }, false, null, overrideInitialTypes: true);

		// Prefer updating the nodes directly instead of calling Graph.RaiseGraphChanged(true) to be sure it is called as soon as possible
		//UpdateNodes(Graph.Nodes.Values.ToList());

		CancelPopup();
	}

	public void ShowLambdaTypeSelector(LambdaGroupModel group, Action<TypeBase> onTypeSelected)
	{
		PopupNode = group.DelegateNode;
		GenericTypeSelectionMenuGeneric = null;
		PopupTypeSelectedAction = onTypeSelected;
		var point = Diagram.GetScreenPoint(group.Position.X + group.Padding, group.Position.Y + 30);
		if (Diagram.Container != null)
			point -= Diagram.Container.NorthWest;
		PopupX = (int)point.X;
		PopupY = (int)point.Y;
		IsShowingGenericTypeSelection = true;
		StateHasChanged();
	}

	#endregion

	#region OnTextboxValueChanged

	public void OnTextboxValueChanged(GraphPortModel port, string? text)
	{
		var connection = port.Connection;

		if (connection.Type.AllowTextboxEdit)
		{
			connection.UpdateTextboxText(text);

			Graph.RaiseGraphChanged(false);
		}
	}

	#endregion

	#region OnNodeDoubleClick

	public void OnNodeDoubleClick(Node node)
	{
		if (node is MethodCall methodCall && methodCall.TargetMethod is NodeClassMethod nodeClassMethod)
		{
			IndexPage.OpenMethod(nodeClassMethod);

			DebuggedPathService.EnterExecutor(node);
		}
	}

	#endregion

	#region SelectionChanged

	private void SelectionChanged(SelectableModel obj)
	{
		foreach (var node in Diagram.Nodes.OfType<GraphNodeModel>())
		{
			if (!obj.Selected && node.IsEditingName)
			{
				node.IsEditingName = false;
				node.Refresh();
			}
		}
	}

	#endregion

	#region Diagram_KeyDown

	private void Diagram_KeyDown(global::Blazor.Diagrams.Core.Events.KeyboardEventArgs obj)
	{
		// Detect f2 key to start editing the name of the selected node
		if (obj.Key == "F2")
		{
			var node = Diagram.Nodes.Where(x => x.Selected).OfType<GraphNodeModel>().FirstOrDefault();
			if (node != null && node.Node.AllowEditingName)
			{
				node.IsEditingName = true;
				node.Refresh();
			}
		}
		// Detect F9 key to toggle breakpoint on the selected node
		else if (obj.Key == "F9")
		{
			var node = Diagram.Nodes.Where(x => x.Selected).OfType<GraphNodeModel>().FirstOrDefault();
			if (node != null && !node.Node.CanBeInlined)
			{
				// If debugging, use Project API to dynamically set/remove breakpoint
				if (Graph.Project.IsHardDebugging)
				{
					if (node.Node.HasBreakpoint)
						Graph.Project.RemoveBreakpointForNode(node.Node.Id);
					else
						Graph.Project.SetBreakpointForNode(node.Node.Id);
				}
				else
				{
					// Not debugging - just toggle decoration
					node.Node.ToggleBreakpoint();
				}
				node.Refresh();
			}
		}
	}

	#endregion

	#endregion

	#region ShowAddNode

	public void ShowAddNode()
	{
		ShowAddNodeAtScope(null, new Vector2(300, 300));
	}

	public void ShowAddNodeForScope(LambdaGroupModel group)
	{
		var bodyPosition = new Vector2(
			(float)(group.Position.X + group.Padding),
			(float)(group.Position.Y + group.Padding + 20));
		ShowAddNodeAtScope(group.DelegateNode.BodyScopeId, bodyPosition);
	}

	private void ShowAddNodeAtScope(string? callableScopeId, Vector2 position)
	{
		PopupNode = null;
		PopupNodeConnection = null;
		PopupCallableScopeId = callableScopeId;
		PopupNodePosition = position;

		if (Diagram.Container != null)
		{
			var screenPosition = Diagram.GetScreenPoint(position.X, position.Y) - Diagram.Container.NorthWest;
			PopupX = (int)screenPosition.X;
			PopupY = (int)screenPosition.Y;
		}
		else
		{
			PopupX = (int)position.X;
			PopupY = (int)position.Y;
		}

		IsShowingNodeSelection = true;
		StateHasChanged();
	}

	public void ShowAddNodeDialog()
	{
		// Same as ShowAddNode but can be called from button click
		ShowAddNode();
	}

	#endregion

	#region CancelPopup

	private void CancelPopup()
	{
		IsShowingGenericTypeSelection = IsShowingNodeSelection = IsShowingOverloadSelection = false;
		PopupNode = null;
		PopupNodeConnection = null;
		PopupCallableScopeId = null;
		PopupTypeSelectedAction = null;
		GenericTypeSelectionMenuGeneric = null;
	}

	#endregion

	#region ToggleBreakpoint

	public void ToggleBreakpointOnSelectedNode()
	{
		var node = Diagram.Nodes.Where(x => x.Selected).OfType<GraphNodeModel>().FirstOrDefault();
		if (node != null && !node.Node.CanBeInlined)
		{
			// If debugging, use Project API to dynamically set/remove breakpoint
			if (Graph.Project.IsHardDebugging)
			{
				if (node.Node.HasBreakpoint)
					Graph.Project.RemoveBreakpointForNode(node.Node.Id);
				else
					Graph.Project.SetBreakpointForNode(node.Node.Id);
			}
			else
			{
				// Not debugging - just toggle decoration
				node.Node.ToggleBreakpoint();
			}
			node.Refresh();
		}
	}

	#endregion

	#region RemoveNode

	public void RemoveNode(Node node)
	{
		if (node is LambdaReturnNode { IsImplicit: true } boundaryReturn)
		{
			var group = FindLambdaGroup(boundaryReturn.CallableScopeId);
			if (group != null)
			{
				foreach (var port in group.Ports
					.OfType<GraphPortModel>()
					.Where(x => x.Connection.Parent == boundaryReturn)
					.ToList())
				{
					group.RemovePort(port);
				}
				group.Refresh();
			}
			return;
		}

		var nodeModel = FindNodeModel(node);
		if (nodeModel == null)
			return;

		var previousNodeSuppression = DisableNodeRemovedUpdate;
		var previousConnectionSuppression = DisableConnectionUpdate;
		DisableNodeRemovedUpdate = true;
		DisableConnectionUpdate = true;
		try
		{
			if (nodeModel is LambdaGroupModel group)
				Diagram.Groups.Remove(group);
			else
				Diagram.Nodes.Remove(nodeModel);
		}
		finally
		{
			DisableNodeRemovedUpdate = previousNodeSuppression;
			DisableConnectionUpdate = previousConnectionSuppression;
		}
	}

	#endregion

	#region AddLink / RemoveLink

	public void RemoveLinkFromGraphCanvas(Connection source, Connection destination)
	{
		var previousConnectionSuppression = DisableConnectionUpdate;
		DisableConnectionUpdate = true;
		try
		{
			var link = Diagram.Links.FirstOrDefault(x => (x.Source.Model as GraphPortModel)?.Connection == source && (x.Target.Model as GraphPortModel)?.Connection == destination);
			if (link != null)
			{
				Diagram.Links.Remove(link);
			}
		}
		finally
		{
			DisableConnectionUpdate = previousConnectionSuppression;
		}
	}

	public void AddLinkToGraphCanvas(Connection source, Connection destination)
	{
		var previousConnectionSuppression = DisableConnectionUpdate;
		DisableConnectionUpdate = true;
		try
		{
			if (source.IsInput)
				(destination, source) = (source, destination);

			var sourcePort = FindPort(source) ?? throw new InvalidOperationException($"No canvas port exists for {source.Parent.Name}.{source.Name}.");
			var destinationPort = FindPort(destination) ?? throw new InvalidOperationException($"No canvas port exists for {destination.Parent.Name}.{destination.Name}.");

			// Make sure there isn't already an existing identical link
			if (Diagram.Links.OfType<LinkModel>().Any(x => (x.Source as SinglePortAnchor)?.Port == sourcePort && (x.Target as SinglePortAnchor)?.Port == destinationPort))
				return;

			var link = Diagram.Links.Add(new LinkModel(sourcePort, destinationPort));

			OnConnectionAdded(link, true);
		}
		finally
		{
			DisableConnectionUpdate = previousConnectionSuppression;
		}
	}

	#endregion

	#region AddNode

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

	private GraphNodeModel AddGraphNodeModel(Node node)
	{
		EnsureInitialScopedPosition(node);
		var nodeModel = Diagram.Nodes.Add(new GraphNodeModel(node));
		foreach (var connection in node.InputsAndOutputs)
			nodeModel.AddPort(new GraphPortModel(nodeModel, connection, node.Inputs.Contains(connection)));

		nodeModel.Moved += OnNodeMoved;
		return nodeModel;
	}

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
			.Count(x => x.Node.CallableScopeId == node.CallableScopeId);
		var offset = new Vector2(groupPadding + existingNodesInScope * 220, groupPadding);
		node.AddDecoration(new NodeDecorationPosition(ownerPosition + offset));
	}

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

	private void AddBoundaryReturnToGroup(LambdaReturnNode boundaryReturn)
	{
		var group = FindLambdaGroup(boundaryReturn.CallableScopeId);
		if (group == null)
			return;

		AddBoundaryReturnPorts(group, boundaryReturn);
		group.Refresh();
	}

	private static void AddBoundaryReturnPorts(LambdaGroupModel group, LambdaReturnNode boundaryReturn)
	{
		foreach (var connection in boundaryReturn.Inputs)
		{
			if (group.Ports.OfType<GraphPortModel>().All(x => x.Connection != connection))
				group.AddPort(new GraphPortModel(group, connection, true));
		}
	}

	private void ReparentScopedModels()
	{
		var groupsByScope = Diagram.Groups
			.OfType<LambdaGroupModel>()
			.ToDictionary(x => x.DelegateNode.BodyScopeId);

		foreach (var nodeModel in Diagram.Nodes.OfType<GraphNodeModel>())
			AttachToScope(nodeModel, nodeModel.Node.CallableScopeId, groupsByScope);

		foreach (var group in Diagram.Groups.OfType<LambdaGroupModel>())
			AttachToScope(group, group.DelegateNode.CallableScopeId, groupsByScope);

		foreach (var rootGroup in Diagram.Groups.OfType<LambdaGroupModel>().Where(x => x.Group == null))
			Diagram.SendToBack(rootGroup);
	}

	private static void AttachToScope(NodeModel model, string? scopeId, IReadOnlyDictionary<string, LambdaGroupModel> groupsByScope)
	{
		groupsByScope.TryGetValue(scopeId ?? string.Empty, out var desiredGroup);
		if (model.Group == desiredGroup)
			return;

		model.Group?.RemoveChild(model);
		desiredGroup?.AddChild(model);
	}

	#endregion

	#region AddNodeLinks

	private void AddNodeLinks(Node node, bool onlyOutputs)
	{
		var addedConnections = new HashSet<(string Source, string Target)>();
		foreach (var connection in onlyOutputs ? node.Outputs : node.InputsAndOutputs) // just process the outputs so we don't connect "input to output" and "output to input" on the same connections
		{
			var portModel = FindPort(connection) ?? throw new InvalidOperationException($"No canvas port exists for {node.Name}.{connection.Name}.");
			foreach (var other in connection.Connections)
			{
				var connectionKey = connection.IsOutput
					? (connection.Id, other.Id)
					: (other.Id, connection.Id);
				if (!addedConnections.Add(connectionKey))
					continue;

				var otherPortModel = FindPort(other) ?? throw new InvalidOperationException($"No canvas port exists for {other.Parent.Name}.{other.Name}.");

				var source = portModel;
				var target = otherPortModel;

				// if we're processing the inputs, we need to swap the source and target to reflect the proper direction
				if (!onlyOutputs && node.Inputs.Contains(connection))
				{
					source = otherPortModel;
					target = portModel;
				}

				// disable the connection update while adding the link so we can call it ourself and 'force' it to be sure it actually runs
				// if we don't do that, we'll have to call it again after adding the link and put the 'force' parameter to true, but then
				// it might be run twice, resulting in all callbacks being called twice!
				var previousConnectionSuppression = DisableConnectionUpdate;
				DisableConnectionUpdate = true;
				LinkModel link;
				try
				{
					link = Diagram.Links.Add(new LinkModel(source, target));
				}
				finally
				{
					DisableConnectionUpdate = previousConnectionSuppression;
				}
				OnConnectionAdded(link, true);

				var connectionWithVertices = GetConnectionContainingVertices(source.Connection, target.Connection);

				if (connectionWithVertices.Vertices.Count != 0)
				{
					Diagram.Batch(() =>
					{
						DisableVertexAddDuringLoading = true;

						foreach (var vertex in connectionWithVertices.Vertices)
							link.AddVertex(new(vertex.X, vertex.Y));

						DisableVertexAddDuringLoading = false;
					});
				}



			}
		}
	}


	#endregion

	#region Refresh

	public void Refresh(Node node)
	{
		if (node is CreateDelegateNode)
		{
			UpdateNodes();
			return;
		}
		if (node is LambdaReturnNode { IsImplicit: true } boundaryReturn)
		{
			var group = FindLambdaGroup(boundaryReturn.CallableScopeId);
			group?.Refresh();
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

		// Type-only updates keep the same Connection instances. Reusing their ports is
		// important because existing diagram links are anchored to those port objects.
		if (portsAreUnchanged)
		{
			nodeModel.Refresh();
			return;
		}

		Diagram.Batch(() =>
		{
			var previousConnectionSuppression = DisableConnectionUpdate;
			DisableConnectionUpdate = true;
			try
			{
				// Links must be removed before their old ports. The core connections remain
				// intact and are re-rendered against the replacement ports below.
				foreach (var link in oldPorts.SelectMany(port => port.Links).Distinct().ToList())
					Diagram.Links.Remove(link);

				foreach (var port in oldPorts)
					nodeModel.RemovePort(port);

				foreach (var expectedPort in expectedPorts)
					nodeModel.AddPort(new GraphPortModel(nodeModel, expectedPort.Connection, expectedPort.IsInput));
			}
			finally
			{
				DisableConnectionUpdate = previousConnectionSuppression;
			}

			AddNodeLinks(node, onlyOutputs: false);
			nodeModel.Refresh();
		});
	}

	#endregion

	#region Initialize

	private void InitializeCanvasWithGraphNodes()
	{
		foreach (var delegateNode in Graph.Nodes.Values.OfType<CreateDelegateNode>())
			AddLambdaGroupModel(delegateNode);

		foreach (var node in Graph.Nodes.Values.Where(x => x is not CreateDelegateNode and not LambdaReturnNode { IsImplicit: true }))
			AddGraphNodeModel(node);

		ReparentScopedModels();

		// add links
		foreach (var node in Graph.Nodes.Values)
			AddNodeLinks(node, true);
	}

	public static string GetTypeShapeColor(TypeBase type, TypeFactory typeFactory)
	{
		if (type.HasUndefinedGenerics)
			return "yellow";
		else if (type == typeFactory.Get<string>())
			return "purple";
		else if (type.IsClass)
			return "green";
		else if (type.IsExec)
			return "gray";
		else if (type == typeFactory.Get<bool>())
			return "red";
		else
			return "blue";
	}

	#endregion

	#region Dispose

	private IDisposable? GraphChangedSubscription;
	private IDisposable? NodeExecutingSubscription;
	private IDisposable? NodeExecutedSubscription;
	public void Dispose()
	{
		GC.SuppressFinalize(this);

		if (Graph.GraphCanvas == this)
			Graph.GraphCanvas = null;

		GraphChangedSubscription?.Dispose();
		NodeExecutingSubscription?.Dispose();
		NodeExecutedSubscription?.Dispose();
		GraphChangedSubscription = null;
		NodeExecutingSubscription = null;
		NodeExecutedSubscription = null;
	}

	#endregion
}
