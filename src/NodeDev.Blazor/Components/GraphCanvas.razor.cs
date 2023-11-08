using Blazor.Diagrams;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Core.PathGenerators;
using Blazor.Diagrams.Core.Routers;
using Blazor.Diagrams.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using NodeDev.Blazor.DiagramsModels;
using NodeDev.Blazor.NodeAttributes;
using NodeDev.Core;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Types;
using System.Numerics;
using System.Reactive.Linq;
using static MudBlazor.CategoryTypes;
using System.Xml.Linq;

namespace NodeDev.Blazor.Components;

public partial class GraphCanvas : Microsoft.AspNetCore.Components.ComponentBase, IDisposable
{
	[Parameter, EditorRequired]
	public Graph Graph { get; set; } = null!;

	private int PopupX = 0;
	private int PopupY = 0;
	private Vector2 PopupNodePosition;
	private Connection? PopupNodeConnection;
	private Node? PopupNode;

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
				DefaultPathGenerator = new SmoothPathGenerator(),
			},
		};
		Diagram = new BlazorDiagram(options);
		Diagram.RegisterComponent<GraphNodeModel, GraphNodeWidget>();

		Diagram.Nodes.Removed += OnNodeRemoved;
		Diagram.Links.Added += OnConnectionAdded;
		Diagram.Links.Removed += OnConnectionRemoved;
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

			GraphChangedSubscription = Graph.SelfClass.Project.GraphChanged.Where(x => x == Graph).AcceptThenSample(TimeSpan.FromMilliseconds(250)).Subscribe(OnGraphChangedFromCore);
		}
	}

	#endregion

	#region OnGraphChangedFromCore

	private void OnGraphChangedFromCore(Graph _)
	{
		InvokeAsync(() =>
		{
			UpdateNodes(Graph.Nodes.Values); // update all the nodes

			StateHasChanged();
		});
	}

	#endregion

	#region UpdateConnectionType

	private void UpdateConnectionType(Connection connection)
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

	#region UpdateNodeBaseInfo

	private void UpdateNodeBaseInfo(Node node)
	{
		var nodeModel = Diagram.Nodes.OfType<GraphNodeModel>().First(x => x.Node == node);
		nodeModel.UpdateNodeBaseInfo(node);
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
				{
					connection.Connections.Remove(input);
				}
			}
			foreach (var output in node.Outputs)
			{
				foreach (var connection in output.Connections)
				{
					connection.Connections.Remove(output);
				}
			}

			Graph.RemoveNode(node);
		});
	}

	#endregion

	#region Connection Added / Removed

	private bool DisableConnectionUpdate = false;
	private void OnConnectionUpdated(BaseLinkModel baseLinkModel, Anchor old, Anchor newAnchor)
	{
		if (DisableConnectionUpdate || baseLinkModel.Source is PositionAnchor || baseLinkModel.Target is PositionAnchor)
			return;

		Graph.Invoke(() =>
		{
			var source = ((GraphPortModel?)baseLinkModel.Source.Model);
			var destination = ((GraphPortModel?)baseLinkModel.Target.Model);

			if (source != null && destination != null)
			{
				if (source.Alignment == PortAlignment.Left) // it's an input, let's swap it so the "source" is an output
				{
					DisableConnectionUpdate = true;
					var old = baseLinkModel.Source;
					baseLinkModel.SetSource(baseLinkModel.Target);
					baseLinkModel.SetTarget(old);
					DisableConnectionUpdate = false;
				}
				source.Connection.Connections.Add(destination.Connection);
				destination.Connection.Connections.Add(source.Connection);

				// we're plugging something something with a generic into something without a generic
				if (source.Connection.Type.HasUndefinedGenerics && !destination.Connection.Type.HasUndefinedGenerics)
				{
					if (source.Connection.Type.IsAssignableTo(destination.Connection.Type, out var newTypes) && newTypes.Count != 0)
					{
						PropagateNewGeneric(source.Connection.Parent, newTypes);
					}
				}
				else if (destination.Connection.Type is UndefinedGenericType destinationType && source.Connection.Type is not UndefinedGenericType)
					PropagateNewGeneric(destination.Connection.Parent, new Dictionary<UndefinedGenericType, TypeBase>() { [destinationType] = source.Connection.Type });

				if (destination.Connection.Connections.Count == 1 && destination.Connection.Type.AllowTextboxEdit) // gotta remove the textbox
					UpdateConnectionType(destination.Connection);

				if (baseLinkModel is LinkModel link && link.Source.Model is GraphPortModel port)
					link.Color = GetTypeShapeColor(port.Connection.Type, port.Connection.Parent.TypeFactory);
			}
		});
	}

	public void OnConnectionAdded(BaseLinkModel baseLinkModel)
	{
		if (DisableConnectionUpdate)
			return;

		baseLinkModel.SourceChanged += OnConnectionUpdated;
		baseLinkModel.TargetChanged += OnConnectionUpdated;
		baseLinkModel.TargetMarker = LinkMarker.Arrow;

		if (baseLinkModel is LinkModel link && link.Source.Model is GraphPortModel port)
			link.Color = GetTypeShapeColor(port.Connection.Type, port.Connection.Parent.TypeFactory);
	}

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
				source.Connections.Remove(destination);
				destination.Connections.Remove(source);

				if (destination.Connections.Count == 0 && destination.Type.AllowTextboxEdit) // gotta add back the textbox
					UpdateConnectionType(destination);
			}
			else
			{

				if (baseLinkModel.Source is PositionAnchor positionAnchor && destination != null)
					OnPortDroppedOnCanvas(destination, Diagram.GetScreenPoint(positionAnchor.GetPlainPosition()!.X, positionAnchor.GetPlainPosition()!.Y) - Diagram.Container!.NorthWest);
				else if (baseLinkModel.Target is PositionAnchor positionAnchor2 && source != null)
					OnPortDroppedOnCanvas(source, Diagram.GetScreenPoint(positionAnchor2.GetPlainPosition()!.X, positionAnchor2.GetPlainPosition()!.Y) - Diagram.Container!.NorthWest);
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
		PopupX = (int)point.X;
		PopupY = (int)point.Y;
		IsShowingNodeSelection = true;

		StateHasChanged();
	}

	private void OnNewNodeTypeSelected(NodeProvider.NodeSearchResult searchResult)
	{
		var node = Graph.AddNode(searchResult);
		node.AddDecoration(new NodeDecorationPosition(new(PopupNodePosition.X, PopupNodePosition.Y)));

		Diagram.Batch(() =>
		{
			if (PopupNodeConnection != null && PopupNode != null)
			{
				// check if the source was an input or output and choose the proper destination based on that
				List<Connection> sources, destinations;
				bool isPopupNodeInput = PopupNode.Inputs.Contains(PopupNodeConnection);
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
					destination = destinations.FirstOrDefault(x => PopupNodeConnection.Type.IsAssignableTo(x.Type, out _) || (x.Type is UndefinedGenericType && !PopupNodeConnection.Type.IsExec));

				// if we found a connection, connect them together
				if (destination != null)
				{
					PopupNodeConnection.Connections.Add(destination);
					destination.Connections.Add(PopupNodeConnection);

					if (destination.Connections.Count == 1 && destination.Type.AllowTextboxEdit)
						UpdateConnectionType(destination);
					if (PopupNodeConnection.Connections.Count == 1 && PopupNodeConnection.Type.AllowTextboxEdit)
						UpdateConnectionType(PopupNodeConnection);

					// check if we need to propagate some generic
					var source = isPopupNodeInput ? destination : PopupNodeConnection;
					var target = isPopupNodeInput ? PopupNodeConnection : destination;
					if (!destination.Type.IsExec && source.Type.IsAssignableTo(target.Type, out var changedGenerics))
					{
						PropagateNewGeneric(node, changedGenerics);
						PropagateNewGeneric(destination.Parent, changedGenerics);
					}
				}
			}

			CancelPopup();

			CreateGraphNodeModel(node);
			AddNodeLinks(node, false);
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

		PopupNode.SelectOverload(overload, out var newConnections, out var removedConnections);

		var nodesToUpdate = new List<Node>();
		foreach (var removedConnection in removedConnections)
		{
			var newConnection = newConnections.FirstOrDefault(x => x.Name == removedConnection.Name && x.Type == removedConnection.Type);

			foreach (var oldLink in removedConnection.Connections)
			{
				// if we found a new connection, connect them together and remove the old connection
				if (newConnection != null)
				{
					newConnection.Connections.Add(oldLink);
					oldLink.Connections.Remove(removedConnection);
					oldLink.Connections.Add(newConnection);
				}

				nodesToUpdate.Add(oldLink.Parent);
			}
		}

		UpdateNodes(nodesToUpdate.Prepend(PopupNode).Distinct());

		CancelPopup();
	}

	#endregion

	#region OnGenericTypeSelectionMenuAsked

	private bool IsShowingGenericTypeSelection = false;
	private UndefinedGenericType? GenericTypeSelectionMenuGeneric;

	public void OnGenericTypeSelectionMenuAsked(GraphNodeModel nodeModel, UndefinedGenericType undefinedGenericType)
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

		PropagateNewGeneric(PopupNode, new Dictionary<UndefinedGenericType, TypeBase>() { [GenericTypeSelectionMenuGeneric] = type });

		UpdateNodes(Graph.Nodes.Values.ToList());

		CancelPopup();
	}

	private void PropagateNewGeneric(Node node, IReadOnlyDictionary<UndefinedGenericType, TypeBase> changedGenerics)
	{
		foreach (var port in node.InputsAndOutputs) // check if any of the ports have the generic we just solved
		{
			if (port.Type.GetUndefinedGenericTypes().Any(changedGenerics.ContainsKey))
			{
				var isPortInput = node.Inputs.Contains(port);

				port.UpdateType(port.Type.ReplaceUndefinedGeneric(changedGenerics));

				UpdateConnectionType(port);

				// check if other connections had their own generics and if we just solved them
				foreach (var other in port.Connections.ToList())
				{
					var source = isPortInput ? other : port;
					var target = isPortInput ? port : other;
					if (source.Type.IsAssignableTo(target.Type, out var changedGenerics2) && changedGenerics2.Count != 0)
						PropagateNewGeneric(other.Parent, changedGenerics2);
					else if((changedGenerics2?.Count ?? 0) != 0)// damn, looks like changing the generic made it so we can't link to this connection anymore
						Graph.Disconnect(port, other);
				}
			}
		}
	}

	#endregion

	#region OnTextboxValueChanged

	[JSInvokable]
	public void OnTextboxValueChanged(string nodeId, string connectionId, string? text)
	{
		if (!Graph.Nodes.TryGetValue(nodeId, out var node))
			return;

		var connection = node.InputsAndOutputs.FirstOrDefault(x => x.Id == connectionId);
		if (connection == null)
			return;

		if (connection.Type.AllowTextboxEdit)
			connection.UpdateTextboxText(text);
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

				var link = Diagram.Links.Add(new LinkModel(source, target));

				OnConnectionAdded(link);
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
	public void Dispose()
	{
		GraphChangedSubscription?.Dispose();
		GraphChangedSubscription = null;
	}

	#endregion
}
