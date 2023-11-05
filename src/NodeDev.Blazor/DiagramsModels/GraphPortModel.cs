using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using NodeDev.Blazor.Components;
using NodeDev.Core.Connections;

namespace NodeDev.Blazor.DiagramsModels;

public class GraphPortModel: PortModel
{
    internal readonly Connection Connection;

    internal string PortColor => GraphCanvas.GetTypeShapeColor(Connection.Type, Connection.Parent.TypeFactory);

    public GraphPortModel(GraphNodeModel parent, Connection connection, bool isInput) : base(parent, isInput ? PortAlignment.Left : PortAlignment.Right)
    {
        Connection = connection;
    }

    public override bool CanAttachTo(ILinkable other)
    {
        if(!base.CanAttachTo(other))
            return false;

        if (other is not GraphPortModel otherPort)
            return false;

        return Connection.Type.IsAssignableTo(otherPort.Connection.Type, out _);
    }
}
