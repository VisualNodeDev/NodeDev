using Blazor.Diagrams;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Core.Routers;
using Blazor.Diagrams.Options;
using Microsoft.AspNetCore.Components;
using NodeDev.Blazor.DiagramsModels;
using NodeDev.Blazor.NodeAttributes;
using NodeDev.Blazor.Services;
using NodeDev.Blazor.Services.GraphManager;
using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
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

	private GraphManagerService? _GraphManagerService;
	private GraphManagerService GraphManagerService => _GraphManagerService ??= new GraphManagerService(this);

	private int PopupX = 0;
	private int PopupY = 0;
	private Vector2 PopupNodePosition;
	private Connection? PopupNodeConnection;
	private Node? PopupNode;

	private GraphNodeModel? SelectedNodeModel { get; set; }

	private BlazorDiagram Diagram { get; set; } = null!;

	#region OnInitialized

	protected override void OnInitialized()
	{
		base.OnInitialized();

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
			await Graph.Invoke(() => Diagram.Batch(InitializeCanvasWithGraphNodes));

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

	#region OnGraphChangedFromCore

	private void OnGraphChangedFromCore((Graph, bool) _)
	{
		InvokeAsync(() =>
		{
			UpdateNodes(Graph.Nodes.Values); // update all the nodes

			StateHasChanged();
		});
	}

	#endregion

	#region UpdateConnectionType

	public void UpdatePortColor(Connection connection)
	{
		var node = Diagram.Nodes.OfType<GraphNodeModel>().FirstOrDefault(x => x.Node == connection.Parent);
		if (node == null)
			return;

		var port = node.GetPort(connection);

		var color = GetTypeShapeColor(connection.Type, node.Node.TypeFactory);
		foreach (LinkModel link in port.Links)
			link.Color = color;

		Diagram.Refresh();
	}

	#endregion

	#region UpdateNodes

	private void UpdateNodes(IEnumerable<Node> nodes)
	{
		Diagram.Batch(() =>
		{
			DisableConnectionUpdate = true;
			DisableNodeRemovedUpdate = true;

			Diagram.Links.Clear();
			Diagram.Nodes.Clear();

			InitializeCanvasWithGraphNodes();

			DisableNodeRemovedUpdate = false;
			DisableConnectionUpdate = false;
		});
	}

	#endregion

	#region Events from client

	#region Node Removed

	bool DisableNodeRemovedUpdate = false;

	public void OnNodeRemoved(NodeModel nodeModel)
	{
		if (DisableNodeRemovedUpdate)
			return;

		Graph.Invoke(() =>
		{
			var node = ((GraphNodeModel)nodeModel).Node;

			foreach (var input in node.Inputs)
			{
				foreach (var connection in input.Connections)
					Graph.Disconnect(input, connection, false);
			}

			foreach (var output in node.Outputs)
			{
				foreach (var connection in output.Connections)
					Graph.Disconnect(output, connection, false);
			}

			Graph.RemoveNode(node, false); // no need to refresh UI, it already came from UI
		});
	}

	#endregion

	#region Connection Added / Removed, Vertex Added / Removed

	private bool DisableConnectionUpdate = false;
	private void OnConnectionUpdated(BaseLinkModel baseLinkModel, Anchor old, Anchor newAnchor)
	{
		if (DisableConnectionUpdate || baseLinkModel.Source is PositionAnchor || baseLinkModel.Target is PositionAnchor)
			return;

		Graph.Invoke(() =>
		{
			var source = ((GraphPortModel?)baseLinkModel.Source.Model);
			var destination = ((GraphPortModel?)baseLinkModel.Target.Model);

			if (source == null || destination == null)
				return;

			if (source.Alignment == PortAlignment.Left) // it's an input, let's swap it so the "source" is an output
			{
				DisableConnectionUpdate = true;
				var old = baseLinkModel.Source;
				baseLinkModel.SetSource(baseLinkModel.Target); // this is necessary as everything assumes that the source is an output and vice versa
				baseLinkModel.SetTarget(old);
				DisableConnectionUpdate = false;

				var tmp = source;
				source = destination;
				destination = tmp;
			}

			GraphManagerService.AddNewConnectionBetween(source.Connection, destination.Connection);
		});
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
	private Connection GetConnectionContainingVertices(Connection source, Connection destination)
	{
		if (source.Type.IsExec) // execs can only have one connection, therefor they always contains the vertex information
			return source;
		else // if this is not an exec, the destination (input) will always contain the vertex information
			return destination;
	}

	private void UpdateVerticesInConnection(Connection source, Connection destination, BaseLinkModel linkModel)
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
			vertex.Moved += _ => Vertex_Moved(link, vertex);

			if (!DisableVertexAddDuringLoading)
				UpdateVerticesInConnection(source.Connection, destination.Connection, link);
		}
	}

	private void Vertex_Moved(LinkModel link, LinkVertexModel vertex)
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

		Graph.Invoke(() =>
		{
			var source = ((GraphPortModel?)baseLinkModel.Source.Model)?.Connection;
			var destination = ((GraphPortModel?)baseLinkModel.Target.Model)?.Connection;

			if (source != null && destination != null)
			{
				Graph.Disconnect(source, destination, false);

				// We have to add back the textbox editor
				if (destination.Connections.Count == 0 && destination.Type.AllowTextboxEdit)
					UpdatePortColor(destination);

				UpdateVerticesInConnection(source, destination, baseLinkModel);
			}
			else
			{

				if (baseLinkModel.Source is PositionAnchor positionAnchor && destination != null)
					OnPortDroppedOnCanvas(destination, positionAnchor.GetPlainPosition()!);
				else if (baseLinkModel.Target is PositionAnchor positionAnchor2 && source != null)
					OnPortDroppedOnCanvas(source, positionAnchor2.GetPlainPosition()!);
			}
		});
	}

	#endregion

	#region Node Moved

	public void OnNodeMoved(MovableModel movableModel)
	{
		var node = ((GraphNodeModel)movableModel).Node;

		var decoration = node.GetOrAddDecoration<NodeDecorationPosition>(() => new(Vector2.Zero));
		decoration.Position = new((float)movableModel.Position.X, (float)movableModel.Position.Y);
	}

	#endregion

	#region OnPortDroppedOnCanvas

	private bool IsShowingNodeSelection = false;

	public void OnPortDroppedOnCanvas(Connection connection, global::Blazor.Diagrams.Core.Geometry.Point point)
	{
		PopupNode = connection.Parent;
		PopupNodeConnection = connection;
		var screenPosition = Diagram.GetScreenPoint(point.X, point.Y) - Diagram.Container!.NorthWest;
		PopupX = (int)screenPosition.X;
		PopupY = (int)screenPosition.Y;
		PopupNodePosition = new((float)point.X, (float)point.Y);
		IsShowingNodeSelection = true;

		StateHasChanged();
	}

	private void OnNewNodeTypeSelected(NodeProvider.NodeSearchResult searchResult)
	{
		var node = Graph.AddNode(searchResult, false);
		node.AddDecoration(new NodeDecorationPosition(new(PopupNodePosition.X, PopupNodePosition.Y)));

		Diagram.Batch(() =>
		{
			CreateGraphNodeModel(node);

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

	#region OnOverloadSelectionRequested

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

		CancelPopup();
	}

	#endregion

	#region OnGenericTypeSelectionMenuAsked

	private bool IsShowingGenericTypeSelection = false;
	private string? GenericTypeSelectionMenuGeneric;

	public void OnGenericTypeSelectionMenuAsked(GraphNodeModel nodeModel, string undefinedGenericType)
	{
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
		if (PopupNode == null || GenericTypeSelectionMenuGeneric == null)
			return;

		GraphManagerService.PropagateNewGeneric(PopupNode, new Dictionary<string, TypeBase>() { [GenericTypeSelectionMenuGeneric] = type }, false, null, overrideInitialTypes: true);

		// Prefer updating the nodes directly instead of calling Graph.RaiseGraphChanged(true) to be sure it is called as soon as possible
		UpdateNodes(Graph.Nodes.Values.ToList());

		CancelPopup();
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
	}

	#endregion

	#region OnNodeRenamed

	internal void OnNodeRenamed(GraphNodeModel node)
	{
		node.IsEditingName = false;

		node.Refresh();

		// When the name of a node changes, refresh the connected nodes in case they also need to refresh
		foreach (var link in node.PortLinks.OfType<LinkModel>())
		{
			if (link.Source.Model is GraphPortModel source)
				source.Parent.Refresh();
			if (link.Target.Model is GraphPortModel target)
				target.Parent.Refresh();
		}
	}

	#endregion

	#endregion

	#region ShowAddNode

	public void ShowAddNode()
	{
		IsShowingNodeSelection = true;
		PopupX = 300;
		PopupY = 300;
	}

	#endregion

	#region CancelPopup

	private void CancelPopup()
	{
		IsShowingGenericTypeSelection = IsShowingNodeSelection = IsShowingOverloadSelection = false;
		PopupNode = null;
		PopupNodeConnection = null;
	}

	#endregion

	#region RemoveLink

	public void RemoveLinkFromGraphCanvas(Connection source, Connection destination)
	{
		Graph.Invoke(() =>
		{
			DisableConnectionUpdate = true;
			try
			{
				Diagram.Links.Remove(Diagram.Links.First(x => (x.Source.Model as GraphPortModel)?.Connection == source && (x.Target.Model as GraphPortModel)?.Connection == destination));
			}
			finally
			{
				DisableConnectionUpdate = false;
			}
		});
	}

	#endregion

	#region CreateGraphNodeModel

	private void CreateGraphNodeModel(Node node)
	{
		var nodeModel = Diagram.Nodes.Add(new GraphNodeModel(node));
		foreach (var connection in node.InputsAndOutputs)
			nodeModel.AddPort(new GraphPortModel(nodeModel, connection, node.Inputs.Contains(connection)));

		nodeModel.Moved += OnNodeMoved;
	}

	#endregion

	#region AddNodeLinks

	private void AddNodeLinks(Node node, bool onlyOutputs)
	{
		var nodeModel = Diagram.Nodes.OfType<GraphNodeModel>().First(x => x.Node == node);
		foreach (var connection in onlyOutputs ? node.Outputs : node.InputsAndOutputs) // just process the outputs so we don't connect "input to output" and "output to input" on the same connections
		{
			var portModel = nodeModel.GetPort(connection);
			foreach (var other in connection.Connections)
			{
				var otherNodeModel = Diagram.Nodes.OfType<GraphNodeModel>().First(x => x.Node == other.Parent);
				var otherPortModel = otherNodeModel.GetPort(other);

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
				DisableConnectionUpdate = true;
				var link = Diagram.Links.Add(new LinkModel(source, target));

				DisableConnectionUpdate = false;
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

	#region Initialize

	private void InitializeCanvasWithGraphNodes()
	{
		// add the nodes themselves
		foreach (var node in Graph.Nodes.Values)
			CreateGraphNodeModel(node);

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
		GraphChangedSubscription?.Dispose();
		NodeExecutingSubscription?.Dispose();
		NodeExecutedSubscription?.Dispose();
		GraphChangedSubscription = null;
		NodeExecutingSubscription = null;
		NodeExecutedSubscription = null;
	}

	#endregion
}
