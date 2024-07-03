using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Flow;

public class EntryNode : FlowNode
{
	public override string TitleColor => "red";

	public EntryNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Entry";

		Outputs.Add(new("Exec", this, TypeFactory.ExecType));

		Outputs.AddRange(graph.SelfMethod.Parameters.Select(x => new Connection(x.Name, this, x.ParameterType)));
	}

	public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => false;

	public override bool DoesOutputPathAllowMerge(Connection execOutput) => throw new NotImplementedException(); // only one exec, doesn't make sense to talk about merging.

	public override string GetExecOutputPathId(string pathId, Connection execOutput) => throw new NotImplementedException();

	public override bool IsFlowNode => true;

	internal void AddNewParameter(NodeClassMethodParameter newParameter)
	{
		Outputs.Add(new Connection(newParameter.Name, this, newParameter.ParameterType));
	}

	internal void RenameParameter(NodeClassMethodParameter parameter, int index)
	{
		Outputs[index + 1].Name = parameter.Name;
	}

	internal Connection UpdateParameterType(NodeClassMethodParameter parameter, int index)
	{
		var connection = Outputs[index + 1];

		connection.UpdateType(parameter.ParameterType);

		return connection;
	}

	internal void Refresh()
	{
		var removedConnections = Outputs.Skip(1).ToList(); // everything except exec
		var newConnections = Graph.SelfMethod.Parameters.Where(x => !x.IsOut).Select(x => new Connection(x.Name, this, x.ParameterType)).ToList();

		Outputs.RemoveRange(1, Outputs.Count - 1);
		Outputs.AddRange(newConnections);

		Graph.MergedRemovedConnectionsWithNewConnections(newConnections, removedConnections);
	}

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		throw new NotImplementedException();
	}

	internal override IEnumerable<(Connection Connection, ParameterExpression LocalVariable)> CreateOutputsLocalVariableExpressions(BuildExpressionInfo info)
	{
		foreach (var output in Outputs.Skip(1))
		{
			yield return (output, info.MethodParametersExpression[output.Name]);
		}
	}
}
