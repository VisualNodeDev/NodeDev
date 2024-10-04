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
        if (source.IsAssignableTo(destination, true, true, out var newTypes, out var usedInitialTypes) && newTypes.Count != 0)
        {
            PropagateNewGeneric(source.Parent, newTypes, usedInitialTypes, destination, false);
            PropagateNewGeneric(destination.Parent, newTypes, usedInitialTypes, source, false);
        }

        GraphCanvas.UpdatePortColor(source);
        GraphCanvas.UpdatePortColor(destination);

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

        GraphCanvas.UpdatePortColor(source);
        GraphCanvas.UpdatePortColor(destination);
    }

    /// <summary>
    /// Propagate the new generic type to all the connections of the node and recursively to the connected nodes.
    /// </summary>
    /// <param name="initiatingConnection">The connection that initiated the propagation. This is used to avoid reupdating back and forth, sometimes erasing information in the process.</param>
    public void PropagateNewGeneric(Node node, IReadOnlyDictionary<UndefinedGenericType, TypeBase> changedGenerics, bool useInitialTypes, Connection? initiatingConnection, bool overrideInitialTypes)
    {
        bool hadAnyChanges = false;
        foreach (var port in node.InputsAndOutputs) // check if any of the ports have the generic we just solved
        {
            var previousType = useInitialTypes ? port.InitialType : port.Type;

            if (!previousType.GetUndefinedGenericTypes().Any(changedGenerics.ContainsKey))
                continue;

            // update port.Type property as well as the textbox visibility if necessary
            port.UpdateTypeAndTextboxVisibility(previousType.ReplaceUndefinedGeneric(changedGenerics), overrideInitialType: overrideInitialTypes);
            hadAnyChanges |= node.GenericConnectionTypeDefined(port).Count != 0;
            GraphCanvas.UpdatePortColor(port);

            var isPortInput = port.IsInput; // cache for performance, IsInput is slow
                                            // check if other connections had their own generics and if we just solved them
            foreach (var other in port.Connections.ToList())
            {
                if(other == initiatingConnection)
                    continue;

                var source = isPortInput ? other : port;
                var target = isPortInput ? port : other;
                if (source.IsAssignableTo(target, isPortInput, !isPortInput, out var changedGenerics2, out var usedInitialTypes) && changedGenerics2.Count != 0)
                    PropagateNewGeneric(other.Parent, changedGenerics2, usedInitialTypes, port, false);
                else if ((changedGenerics2?.Count ?? 0) != 0)// looks like changing the generic made it so we can't link to this connection anymore
                    DisconnectConnectionBetween(port, other);
            }
        }

        if (hadAnyChanges)
            Graph.RaiseGraphChanged(false);
    }

    public void SelectNodeOverload(Node popupNode, Node.AlternateOverload overload)
    {
        popupNode.SelectOverload(overload, out var newConnections, out var removedConnections);

        Graph.MergeRemovedConnectionsWithNewConnections(newConnections, removedConnections);
    }
}
