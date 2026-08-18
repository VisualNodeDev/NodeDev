using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using NodeDev.Blazor.Components;
using NodeDev.Core.Connections;

namespace NodeDev.Blazor.DiagramsModels;

public class GraphPortModel : PortModel
{
	internal readonly Connection Connection;

	internal int ExecutionCount = 0;

	internal string PortColor => GraphCanvas.GetTypeShapeColor(Connection.Type, Connection.Parent.TypeFactory);

	public GraphPortModel(NodeModel parent, Connection connection, bool isInput) : base(parent, isInput ? PortAlignment.Left : PortAlignment.Right)
	{
		Connection = connection;
	}

	public override bool CanAttachTo(ILinkable other)
	{
		if (!base.CanAttachTo(other))
			return false;

		if (other is not GraphPortModel otherPort)
			return false;

		if (Alignment == otherPort.Alignment) // can't plug input to input or output to output
			return false;

		var source = Connection.IsOutput ? Connection : otherPort.Connection;
		var destination = Connection.IsInput ? Connection : otherPort.Connection;
		if (source.Parent.CallableScopeId != destination.Parent.CallableScopeId)
		{
			if (source.Type.IsExec || destination.Type.IsExec)
				return false;
			if (!source.Parent.Graph.IsScopeAncestorOf(source.Parent.CallableScopeId, destination.Parent.CallableScopeId))
				return false;
		}

		return source.Type.IsAssignableTo(destination.Type, out _, out _);
	}
}
