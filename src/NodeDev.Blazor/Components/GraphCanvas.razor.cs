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

	private BlazorDiagram Diagram { get; set; } = null!;
	private GraphDiagramProjection DiagramProjection { get; set; } = null!;
	private GraphDiagramSynchronizer DiagramSynchronizer { get; set; } = null!;
	private GraphPopupState PopupState { get; } = new();
	private GraphDebugVisualizer DebugVisualizer { get; set; } = null!;

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

		DiagramSynchronizer = new GraphDiagramSynchronizer(Graph, action => InvokeAsync(action));
		DiagramProjection = new GraphDiagramProjection(Graph, Diagram, DiagramSynchronizer, OnConnectionAdded);
		DebugVisualizer = new GraphDebugVisualizer(Graph, Diagram, action => InvokeAsync(action));
	}

	#endregion

	#region OnAfterRenderAsync

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await base.OnAfterRenderAsync(firstRender);

		if (firstRender)
		{
			await Task.Delay(100);
			Diagram.Batch(DiagramProjection.Initialize);
			DiagramSynchronizer.Start(DiagramProjection.Rebuild, StateHasChanged);
			DebugVisualizer.Start();
		}
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

	private NodeModel? FindNodeModel(Node node)
	{
		return DiagramProjection.FindNodeModel(node);
	}

	private GraphPortModel? FindPort(Connection connection)
	{
		return DiagramProjection.FindPort(connection);
	}

	private LambdaGroupModel? FindLambdaGroup(string? bodyScopeId) => DiagramProjection.FindLambdaGroup(bodyScopeId);

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

	public void OnNodeRemoved(NodeModel nodeModel)
	{
		if (DiagramSynchronizer.IsNodeRemovalSuppressed)
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

	private void OnConnectionUpdated(BaseLinkModel baseLinkModel, Anchor old, Anchor newAnchor)
	{
		if (DiagramSynchronizer.IsConnectionUpdateSuppressed || baseLinkModel.Source is PositionAnchor || baseLinkModel.Target is PositionAnchor)
			return;

		var source = ((GraphPortModel?)baseLinkModel.Source.Model);
		var destination = ((GraphPortModel?)baseLinkModel.Target.Model);

		if (source == null || destination == null)
			return;

		if (source.Alignment == PortAlignment.Left) // it's an input, let's swap it so the "source" is an output
		{
			using (DiagramSynchronizer.SuppressConnectionUpdates())
			{
				var old2 = baseLinkModel.Source;
				baseLinkModel.SetSource(baseLinkModel.Target); // this is necessary as everything assumes that the source is an output and vice versa
				baseLinkModel.SetTarget(old2);
			}

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
		if (DiagramSynchronizer.IsConnectionUpdateSuppressed && !force)
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
	internal static Connection GetConnectionContainingVertices(Connection source, Connection destination)
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

			if (!DiagramSynchronizer.IsLoadingVertices)
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
		if (DiagramSynchronizer.IsConnectionUpdateSuppressed)
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

	#endregion

	#region OnPortDroppedOnCanvas

	public void OnPortDroppedOnCanvas(Connection connection, global::Blazor.Diagrams.Core.Geometry.Point point)
	{
		var screenPosition = Diagram.GetScreenPoint(point.X, point.Y) - Diagram.Container!.NorthWest;
		PopupState.ShowNodeSelection(
			(int)screenPosition.X,
			(int)screenPosition.Y,
			new((float)point.X, (float)point.Y),
			connection,
			connection.Parent,
			connection.Parent.CallableScopeId);

		StateHasChanged();
	}

	private void OnNewNodeTypeSelected(NodeProvider.NodeSearchResult searchResult)
	{
		var node = GraphManagerService.AddNode(searchResult, node =>
		{
			node.AddDecoration(new NodeDecorationPosition(PopupState.NodePosition));
		}, PopupState.CallableScopeId);

		Diagram.Batch(() =>
		{
			if (PopupState.NodeConnection is { } popupConnection && PopupState.Node is { } popupNode)
			{
				// check if the source was an input or output and choose the proper destination based on that
				List<Connection> sources, destinations;
				bool isPopupNodeInput = popupConnection.IsInput;
				if (isPopupNodeInput)
				{
					sources = popupNode.Inputs;
					destinations = node.Outputs;
				}
				else
				{
					sources = popupNode.Outputs;
					destinations = node.Inputs;
				}

				Connection? destination = null;
				if (popupConnection.Type is UndefinedGenericType) // can connect to anything except exec
					destination = destinations.FirstOrDefault(x => !x.Type.IsExec);
				else // can connect to anything that is assignable to the type
					destination = destinations.FirstOrDefault(x => popupConnection.Type.IsAssignableTo(x.Type, out _, out _) || (x.Type is UndefinedGenericType && !popupConnection.Type.IsExec));

				// if we found a connection, connect them together
				if (destination != null)
				{
					var source = isPopupNodeInput ? destination : popupConnection;
					var target = isPopupNodeInput ? popupConnection : destination;

					GraphManagerService.AddNewConnectionBetween(source, target);
				}
			}

			CancelPopup();
		});

	}

	#endregion

	#region OnOverloadSelectionRequested / OnNewOverloadSelected

	public void OnOverloadSelectionRequested(GraphNodeModel graphNode)
	{
		PopupState.ShowOverloadSelection(graphNode.Node);

		StateHasChanged();
	}

	private void OnNewOverloadSelected(Node.AlternateOverload overload)
	{
		if (PopupState.Node == null)
			return;

		GraphManagerService.SelectNodeOverload(PopupState.Node, overload);

		// Refresh the node visually after overload selection
		// The node's ports have changed, so we need to update the UI
		Refresh(PopupState.Node);

		CancelPopup();
	}

	#endregion

	#region OnGenericTypeSelectionMenuAsked

	public void OnGenericTypeSelectionMenuAsked(GraphNodeModel nodeModel, string undefinedGenericType)
	{
		var p = Diagram.GetScreenPoint(nodeModel.Position.X, nodeModel.Position.Y) - Diagram.Container!.NorthWest;
		PopupState.ShowGenericTypeSelection((int)p.X, (int)p.Y, nodeModel.Node, undefinedGenericType);

		StateHasChanged();
	}

	private void OnGenericTypeSelected(TypeBase type)
	{
		if (PopupState.TypeSelectedAction != null)
		{
			PopupState.TypeSelectedAction(type);
			CancelPopup();
			return;
		}

		if (PopupState.Node == null || PopupState.GenericTypeName == null)
			return;

		GraphManagerService.PropagateNewGeneric(PopupState.Node, new Dictionary<string, TypeBase>() { [PopupState.GenericTypeName] = type }, false, null, overrideInitialTypes: true);

		CancelPopup();
	}

	public void ShowLambdaTypeSelector(LambdaGroupModel group, Action<TypeBase> onTypeSelected)
	{
		var point = Diagram.GetScreenPoint(group.Position.X + group.Padding, group.Position.Y + 30);
		if (Diagram.Container != null)
			point -= Diagram.Container.NorthWest;
		PopupState.ShowGenericTypeSelection((int)point.X, (int)point.Y, group.DelegateNode, null, onTypeSelected);
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
		int x;
		int y;
		if (Diagram.Container != null)
		{
			var screenPosition = Diagram.GetScreenPoint(position.X, position.Y) - Diagram.Container.NorthWest;
			x = (int)screenPosition.X;
			y = (int)screenPosition.Y;
		}
		else
		{
			x = (int)position.X;
			y = (int)position.Y;
		}

		PopupState.ShowNodeSelection(x, y, position, null, null, callableScopeId);
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
		PopupState.Reset();
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

		using var suppression = DiagramSynchronizer.SuppressCanvasMutations();
		if (nodeModel is LambdaGroupModel modelGroup)
			Diagram.Groups.Remove(modelGroup);
		else
			Diagram.Nodes.Remove(nodeModel);
	}

	#endregion

	#region AddLink / RemoveLink

	public void RemoveLinkFromGraphCanvas(Connection source, Connection destination)
	{
		using (DiagramSynchronizer.SuppressConnectionUpdates())
		{
			var link = Diagram.Links.FirstOrDefault(x => (x.Source.Model as GraphPortModel)?.Connection == source && (x.Target.Model as GraphPortModel)?.Connection == destination);
			if (link != null)
				Diagram.Links.Remove(link);
		}
	}

	public void AddLinkToGraphCanvas(Connection source, Connection destination)
	{
		using (DiagramSynchronizer.SuppressConnectionUpdates())
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
	}

	#endregion

	#region AddNode

	public void AddNode(Node node)
	{
		DiagramProjection.AddNode(node);
	}

	#endregion

	#region Refresh

	public void Refresh(Node node)
	{
		DiagramProjection.Refresh(node);
	}

	#endregion

	#region Initialize

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

	public void Dispose()
	{
		GC.SuppressFinalize(this);

		if (Graph.GraphCanvas == this)
			Graph.GraphCanvas = null;

		DebugVisualizer.Dispose();
		DiagramSynchronizer.Dispose();
	}

	#endregion
}
