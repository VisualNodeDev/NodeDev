﻿using Blazor.Diagrams;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Core.Routers;
using Blazor.Diagrams.Options;
using Microsoft.AspNetCore.Components;
using NodeDev.Blazor.DiagramsModels;
using NodeDev.Blazor.NodeAttributes;
using NodeDev.Core;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Types;
using System.Numerics;
using System.Reactive.Linq;
using NodeDev.Core.Class;
using NodeDev.Blazor.Services;

namespace NodeDev.Blazor.Components;

public partial class GraphCanvas : ComponentBase, IDisposable
{
    [Parameter, EditorRequired]
    public Graph Graph { get; set; } = null!;

    [CascadingParameter]
    public Index IndexPage { get; set; } = null!;

    [Inject]
    internal DebuggedPathService DebuggedPathService { get; set; } = null!;

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
                baseLinkModel.SetSource(baseLinkModel.Target);
                baseLinkModel.SetTarget(old);
                DisableConnectionUpdate = false;

                var tmp = source;
                source = destination;
                destination = tmp;
            }

            Graph.Connect(source.Connection, destination.Connection, false);

            // we're plugging something something with a generic into something without a generic
            if (source.Connection.Type.HasUndefinedGenerics && !destination.Connection.Type.HasUndefinedGenerics)
            {
                if (source.Connection.Type.IsAssignableTo(destination.Connection.Type, out var newTypes) && newTypes.Count != 0)
                {
                    PropagateNewGeneric(source.Connection.Parent, newTypes, false);
                }
            }
            else if (destination.Connection.Type.HasUndefinedGenerics && !source.Connection.Type.HasUndefinedGenerics)
            {
                if (source.Connection.Type.IsAssignableTo(destination.Connection.Type, out var newTypes) && newTypes.Count != 0)
                {
                    PropagateNewGeneric(destination.Connection.Parent, newTypes, false);
                }
            }

            // we have to remove the textbox ?
            if (destination.Connection.Connections.Count == 1 && destination.Connection.Type.AllowTextboxEdit)
                UpdateConnectionType(destination.Connection);

            if (baseLinkModel is LinkModel link && link.Source.Model is GraphPortModel port)
            {
                link.Color = GetTypeShapeColor(port.Connection.Type, port.Connection.Parent.TypeFactory);

                // we have to disconnect the previously connected exec, since exec outputs can only have one connection
                if (source.Connection.Type.IsExec && source.Connection.Connections.Count > 1 && link.Target.Model is GraphPortModel target)
                {
                    Diagram.Links.Remove(Diagram.Links.First(x => (x.Source.Model as GraphPortModel)?.Connection == source.Connection && (x.Target.Model as GraphPortModel)?.Connection != target.Connection));
                    Graph.Disconnect(source.Connection, source.Connection.Connections.First(x => x != target.Connection), false);
                }

            }

            UpdateVerticesInConnection(source.Connection, destination.Connection, baseLinkModel);

            Graph.RaiseGraphChanged(true);
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
                    UpdateConnectionType(destination);

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
                    Graph.Connect(PopupNodeConnection, destination, false);

                    if (destination.Connections.Count == 1 && destination.Type.AllowTextboxEdit)
                        UpdateConnectionType(destination);
                    if (PopupNodeConnection.Connections.Count == 1 && PopupNodeConnection.Type.AllowTextboxEdit)
                        UpdateConnectionType(PopupNodeConnection);

                    var source = isPopupNodeInput ? destination : PopupNodeConnection;
                    var target = isPopupNodeInput ? PopupNodeConnection : destination;

                    // check if we need to propagate some generic
                    if (!destination.Type.IsExec && source.Type.IsAssignableTo(target.Type, out var changedGenerics))
                    {
                        PropagateNewGeneric(node, changedGenerics, false);
                        PropagateNewGeneric(destination.Parent, changedGenerics, false);
                    }
                    else if (source.Type.IsExec && source.Connections.Count > 1) // check if we have to disconnect the previously connected exec
                    {
                        Diagram.Links.Remove(Diagram.Links.First(x => (x.Source.Model as GraphPortModel)?.Connection == source && (x.Target.Model as GraphPortModel)?.Connection != target));
                        var toRemove = source.Connections.FirstOrDefault(x => x != target);
                        if (toRemove != null)
                            Graph.Disconnect(source, toRemove, false);
                    }
                }
            }

            CancelPopup();

            CreateGraphNodeModel(node);
            AddNodeLinks(node, false);

            UpdateNodes(Graph.Nodes.Values.ToList());
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

        Graph.MergedRemovedConnectionsWithNewConnections(newConnections, removedConnections);

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

        PropagateNewGeneric(PopupNode, new Dictionary<UndefinedGenericType, TypeBase>() { [GenericTypeSelectionMenuGeneric] = type }, false);

        // Prefer updating the nodes directly instead of calling Graph.RaiseGraphChanged(true) to be sure it is called as soon as possible
        UpdateNodes(Graph.Nodes.Values.ToList());

        CancelPopup();
    }

    private void PropagateNewGeneric(Node node, IReadOnlyDictionary<UndefinedGenericType, TypeBase> changedGenerics, bool requireUIRefresh)
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
                        PropagateNewGeneric(other.Parent, changedGenerics2, requireUIRefresh);
                    else if ((changedGenerics2?.Count ?? 0) != 0)// damn, looks like changing the generic made it so we can't link to this connection anymore
                        Graph.Disconnect(port, other, false); // no need to refresh UI here as it'll already be refresh at the end of this method
                }
            }
        }

        Graph.RaiseGraphChanged(requireUIRefresh);
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
        var nodeModel = Diagram.Nodes.FirstOrDefault(x => x.Selected) as GraphNodeModel;

        try
        {
            var path = nodeModel?.Node.SearchAllExecPaths([]);

            foreach (var otherNodeModel in Diagram.Nodes.OfType<GraphNodeModel>())
            {
                foreach (var connection in otherNodeModel.Node.Outputs)
                {
                    if (path != null && path.Contains(connection))
                        otherNodeModel.OnConnectionPathHighlighted(connection);
                    else
                        otherNodeModel.OnConnectionPathUnhighlighted(connection);
                }

            }
        }
        catch (Node.InfiniteLoopException)
        { }
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
