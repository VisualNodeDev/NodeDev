using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using NodeDev.Blazor.NodeAttributes;
using NodeDev.Core;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NodeDev.Blazor.Components
{
    public partial class GraphCanvas : ComponentBase, IDisposable
    {
        [Inject]
        private IJSRuntime JS { get; set; } = null!;


        private readonly string Id = Guid.NewGuid().ToString();
        private DotNetObjectReference<GraphCanvas> Ref = null!;

        [Parameter, EditorRequired]
        public Graph Graph { get; set; } = null!;

        private string CanvasJS => $"Canvas_{Id}";

        private List<Node> SelectedNodes { get; } = new();

        private int PopupX = 0;
        private int PopupY = 0;
        private Connection? PopupNodeConnection;
        private Node? PopupNode;

        private ValueTask InvokeJSVoid(string name, params object?[]? parameters) => JS.InvokeVoidAsync($"{CanvasJS}.{name}", parameters);
        private ValueTask<T> InvokeJS<T>(string name, params object?[]? parameters) => JS.InvokeAsync<T>($"{CanvasJS}.{name}", parameters);

        #region OnAfterRenderAsync

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                Ref = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("InitializeCanvas", Ref, Id);

                await Task.Delay(100);
                await Graph.Invoke(InitializeCanvasWithGraphNodes);
            }
        }

        #endregion

        #region Events from client

        #region Node Selected / unselected

        [JSInvokable]
        public void OnNodeSelectedInClient(string nodeId)
        {
            if (Graph.Nodes.TryGetValue(nodeId, out var node))
                SelectedNodes.Add(node);
        }

        [JSInvokable]
        public void OnNodeUnselectedInClient(string nodeId)
        {
            if (Graph.Nodes.TryGetValue(nodeId, out var node))
                SelectedNodes.Remove(node);
        }

        #endregion

        #region Node Removed

        [JSInvokable]
        public void OnNodeRemoved(string nodeId)
        {
            Graph.Invoke(() =>
            {
                if (!Graph.Nodes.TryGetValue(nodeId, out var node))
                    return;
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

        [JSInvokable]
        public void OnConnectionAdded(string nodeSourceId, string outputId, string nodeDestinationID, string inputId)
        {
            Graph.Invoke(() =>
            {
                if (Graph.Nodes.TryGetValue(nodeSourceId, out var nodeSource) && Graph.Nodes.TryGetValue(nodeDestinationID, out var nodeDestination))
                {
                    var source = nodeSource.Outputs.FirstOrDefault(x => x.Id == outputId);
                    var destination = nodeDestination.Inputs.FirstOrDefault(x => x.Id == inputId);

                    if (source != null && destination != null)
                    {
                        source.Connections.Add(destination);
                        destination.Connections.Add(source);

                        if (source.Type.IsGeneric && !destination.Type.IsGeneric)
                            PropagateNewGeneric(nodeSource, (UndefinedGenericType)source.Type, destination.Type);
                        else if(destination.Type.IsGeneric && !source.Type.IsGeneric)
                            PropagateNewGeneric(nodeDestination, (UndefinedGenericType)destination.Type, source.Type);
                    }
                }
            });
        }

        [JSInvokable]
        public void OnConnectionRemoved(string nodeSourceId, string outputId, string nodeDestinationID, string inputId)
        {
            Graph.Invoke(() =>
            {
                if (Graph.Nodes.TryGetValue(nodeSourceId, out var nodeSource) && Graph.Nodes.TryGetValue(nodeDestinationID, out var nodeDestination))
                {
                    var source = nodeSource.InputsAndOutputs.FirstOrDefault(x => x.Id == outputId || x.Id == inputId);
                    var destination = nodeDestination.InputsAndOutputs.FirstOrDefault(x => x.Id == inputId || x.Id == outputId);

                    if (source != null && destination != null)
                    {
                        source.Connections.Remove(destination);
                        destination.Connections.Remove(source);
                    }
                }
            });
        }

        #endregion

        #region Node Moved

        [JSInvokable]
        public void OnNodeMoved(string nodeId, float x, float y)
        {
            if (!Graph.Nodes.TryGetValue(nodeId, out var node))
                return;
            var decoration = node.GetOrAddDecoration<NodeDecorationPosition>(() => new(Vector2.Zero));
            decoration.Position = new(x, y);
        }

        #endregion

        #region OnPortDroppedOnCanvas

        private bool IsShowingNodeSelection = false;

        [JSInvokable]
        public void OnPortDroppedOnCanvas(string nodeId, string connectionId, int x, int y)
        {
            if (!Graph.Nodes.TryGetValue(nodeId, out var node))
                return;

            var connection = node.InputsAndOutputs.FirstOrDefault(x => x.Id == connectionId);
            if (connection == null)
                return;

            PopupNode = node;
            PopupNodeConnection = connection;
            PopupX = x;
            PopupY = y;
            IsShowingNodeSelection = true;

            StateHasChanged();
        }

        private void OnNewNodeTypeSelected(Type typeSelected)
        {
            IsShowingNodeSelection = false; // remove the node type selection popup

            var node = Graph.AddNode(typeSelected);
            node.AddDecoration(new NodeDecorationPosition(new(PopupX, PopupY)));

            if (PopupNodeConnection != null && PopupNode != null)
            {
                // check if the source was an input or output and choose the proper destination based on that
                List<Connection> sources, destinations;
                if (PopupNode.Inputs.Contains(PopupNodeConnection))
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
                if (PopupNodeConnection.Type.IsGeneric) // can connect to anything except exec
                    destination = destinations.FirstOrDefault(x => !x.Type.IsExec);
                else // can connect to anything that is the same type or generic (except exec)
                    destination = destinations.FirstOrDefault(x => x.Type == PopupNodeConnection.Type || (x.Type.IsGeneric && !PopupNodeConnection.Type.IsExec));

                // if we found a connection, connect them together
                if (destination != null)
                {
                    PopupNodeConnection.Connections.Add(destination);
                    destination.Connections.Add(PopupNodeConnection);

                    // if one of the connection ( destination or PopupNodeConnection ) is generic and the other isn't
                    // We have to propagate the non-generic type to the generic one
                    if (destination.Type.IsGeneric && !PopupNodeConnection.Type.IsGeneric)
                        PropagateNewGeneric(destination.Parent, (UndefinedGenericType)destination.Type, PopupNodeConnection.Type);
                    else if (!destination.Type.IsGeneric && PopupNodeConnection.Type.IsGeneric)
                        PropagateNewGeneric(PopupNodeConnection.Parent, (UndefinedGenericType)PopupNodeConnection.Type, destination.Type);
                }
            }

            InvokeJSVoid("AddNodes", GetNodeCreationInfo(node)).AndForget();
        }

        #endregion

        #region OnGenericTypeSelectionMenuAsked

        private bool IsShowingGenericTypeSelection = false;

        [JSInvokable]
        public void OnGenericTypeSelectionMenuAsked(string nodeId, string connectionId, int x, int y)
        {
            if (!Graph.Nodes.TryGetValue(nodeId, out var node))
                return;
            var connection = node.InputsAndOutputs.FirstOrDefault(x => x.Id == connectionId);
            if (connection == null)
                return;

            PopupNode = node;
            PopupNodeConnection = connection;
            PopupX = x;
            PopupY = y;
            IsShowingGenericTypeSelection = true;

            StateHasChanged();
        }

        private void OnGenericTypeSelected(Type type)
        {
            IsShowingGenericTypeSelection = false;

            if (PopupNode == null || PopupNodeConnection?.Type is not UndefinedGenericType generic)
                return;

            PropagateNewGeneric(PopupNode, generic, TypeFactory.Get(type));
        }

        private record class UpdateConnectionTypeParameters(string NodeId, string Id, string Type, bool IsGeneric, string Color, bool AllowTextboxEdit, string? TextboxValue);
        private void PropagateNewGeneric(Node node, UndefinedGenericType generic, TypeBase newType)
        {
            foreach (var connection in node.InputsAndOutputs)
            {
                if (connection.Type == generic)
                {
                    connection.UpdateType(newType);
                    
                    InvokeJSVoid("UpdateConnectionType", new UpdateConnectionTypeParameters(node.Id, connection.Id, newType.Name, false, GetTypeShapeColor(newType), newType.AllowTextboxEdit, connection.TextboxValue)).AndForget();

                    foreach (var other in connection.Connections)
                    {
                        if (other.Type is UndefinedGenericType generic2)
                            PropagateNewGeneric(other.Parent, generic2, newType);
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

            if(connection.Type.AllowTextboxEdit)
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

        #region Initialize

        private async Task InitializeCanvasWithGraphNodes()
        {
            var infos = Graph.Nodes.Values.Select(GetNodeCreationInfo).ToList();

            await InvokeJSVoid("AddNodes", infos);
        }

        private record class NodeCreationInfo(string Id, string Name, float X, float Y, List<NodeCreationInfo_Connection> Inputs, List<NodeCreationInfo_Connection> Outputs);
        private record class NodeCreationInfo_Connection(string Id, string Name, List<NodeCreationInfo_Connection_Connection>? Connections, string Color, string Type, bool IsGeneric, bool AllowTextboxEdit, string? TextboxValue);
        private record class NodeCreationInfo_Connection_Connection(string ConnectionId, string NodeId);

        private string GetTypeShapeColor(TypeBase type)
        {
            if (type.IsGeneric)
                return "yellow";
            if (type.IsClass)
                return "green";
            else if (type.IsExec)
                return "gray";
            else
                return "blue";
        }

        private NodeCreationInfo GetNodeCreationInfo(Node node)
        {
            var positionAttribute = node.GetOrAddDecoration<NodeDecorationPosition>(() => new(Vector2.Zero));

            return new(node.Id.ToString(),
                node.Name,
                positionAttribute.X,
                positionAttribute.Y,
                node.Inputs.Select(x => new NodeCreationInfo_Connection(x.Id, x.Name, x.Connections.Select(y => new NodeCreationInfo_Connection_Connection(y.Id, y.Parent.Id)).ToList(), GetTypeShapeColor(x.Type), x.Type.Name, x.Type.IsGeneric, x.Type.AllowTextboxEdit, x.TextboxValue)).ToList(),
                node.Outputs.Select(x => new NodeCreationInfo_Connection(x.Id, x.Name, x.Connections.Select(y => new NodeCreationInfo_Connection_Connection(y.Id, y.Parent.Id)).ToList(), GetTypeShapeColor(x.Type), x.Type.Name, x.Type.IsGeneric, false, null)).ToList());
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            Ref?.Dispose();
            Ref = null!;
        }

        #endregion
    }
}
