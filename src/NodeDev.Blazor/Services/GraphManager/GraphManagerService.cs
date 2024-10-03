using NodeDev.Blazor.DiagramsModels;
using NodeDev.Core;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Types;

namespace NodeDev.Blazor.Services.GraphManager;

/// <summary>
/// Contains the logic on how to manipulate Graphs and Nodes.
/// </summary>
public class GraphManagerService
{
    private readonly IGraphCanvas GraphCanvas;

    private Graph Graph => GraphCanvas.Graph;

    public GraphManagerService(IGraphCanvas graphCanvas)
    {
        GraphCanvas = graphCanvas;
    }

    public void AddNewConnectionBetween(Connection source, Connection destination)
    {
        Graph.Connect(source, destination, false);

        // we're plugging something something with a generic into something without a generic
        if (source.Type.IsAssignableTo(destination.Type, out var newTypes) && newTypes.Count != 0)
        {
            PropagateNewGeneric(source.Parent, newTypes, false);
            PropagateNewGeneric(destination.Parent, newTypes, false);
        }

        GraphCanvas.UpdatePortTypeAndColor(source);
        GraphCanvas.UpdatePortTypeAndColor(destination);

        // we have to disconnect the previously connected exec, since exec outputs can only have one connection
        if (source.Type.IsExec && source.Connections.Count > 1)
            DisconnectConnectionBetween(source, source.Connections.First(x => x != destination));
        else if (!destination.Type.IsExec && destination.Connections.Count > 1) // non-exec inputs can only have one connection
            DisconnectConnectionBetween(destination.Connections.First(x => x != source), destination);

        Graph.RaiseGraphChanged(true);
    }

    public void DisconnectConnectionBetween(Connection source, Connection destination)
    {
        Graph.Disconnect(source, destination, false);
        GraphCanvas.RemoveLinkFromGraphCanvas(source, destination);

        GraphCanvas.UpdatePortTypeAndColor(source);
        GraphCanvas.UpdatePortTypeAndColor(destination);
    }

    public void PropagateNewGeneric(Node node, IReadOnlyDictionary<UndefinedGenericType, TypeBase> changedGenerics, bool requireUIRefresh)
    {
        foreach (var port in node.InputsAndOutputs) // check if any of the ports have the generic we just solved
        {
            var previousType = port.Type;

            if (!previousType.GetUndefinedGenericTypes().Any(changedGenerics.ContainsKey))
                continue;

            // update port.Type property as well as the textbox visibility if necessary
            port.UpdateTypeAndTextboxVisibility(previousType.ReplaceUndefinedGeneric(changedGenerics));
            node.GenericConnectionTypeDefined(port);
            GraphCanvas.UpdatePortTypeAndColor(port);

            var isPortInput = port.IsInput; // cache for performance, IsInput is slow
                                            // check if other connections had their own generics and if we just solved them
            foreach (var other in port.Connections.ToList())
            {
                var source = isPortInput ? other : port;
                var target = isPortInput ? port : other;
                if (source.Type.IsAssignableTo(target.Type, out var changedGenerics2) && changedGenerics2.Count != 0)
                    PropagateNewGeneric(other.Parent, changedGenerics2, false); // no need to refresh UI since we'll do it ourselves anyway at the end of this call
                else if ((changedGenerics2?.Count ?? 0) != 0)// looks like changing the generic made it so we can't link to this connection anymore
                    DisconnectConnectionBetween(port, other);
            }
        }

        Graph.RaiseGraphChanged(requireUIRefresh);
    }

    public void SelectNodeOverload(Node popupNode, Node.AlternateOverload overload)
    {
        popupNode.SelectOverload(overload, out var newConnections, out var removedConnections);

        Graph.MergeRemovedConnectionsWithNewConnections(newConnections, removedConnections);
    }
}
