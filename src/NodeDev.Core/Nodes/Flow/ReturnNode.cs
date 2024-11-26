using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace NodeDev.Core.Nodes.Flow;

public class ReturnNode : FlowNode
{
	public override string TitleColor => "red";

	public override bool BreaksDeadEnd => true;

	public ReturnNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Return";

		Inputs.Add(new("Exec", this, TypeFactory.ExecType));

		Refresh();
	}

	private bool HasReturnValue => Graph.SelfMethod.ReturnType != TypeFactory.Void;

	public override bool IsFlowNode => throw new NotImplementedException();

	public override string GetExecOutputPathId(string pathId, Connection execOutput) => throw new NotImplementedException();

	public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => true; // Return by definition bypasses any dead-end restrictions

	public override bool DoesOutputPathAllowMerge(Connection execOutput) => throw new NotImplementedException();

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		// Assign any out parameters before returning
		var inputs = CollectionsMarshal.AsSpan(Inputs)[1..^(HasReturnValue ? 1 : 0)];
		var assigns = new List<Expression>(inputs.Length);

		foreach (var input in inputs)
		{
			var valueExpression = info.LocalVariables[input];

			var assign = Expression.Assign(info.MethodParametersExpression[input.Name], valueExpression);

			assigns.Add(assign);
		}

		if (HasReturnValue)
			return Expression.Return(info.ReturnLabel, info.LocalVariables[Inputs[^1]]);
		else
			return Expression.Return(info.ReturnLabel);
	}

	internal void Refresh()
	{
		var removedConnections = Inputs.Skip(1).ToList(); // everything except exec
		var newConnections = Graph.SelfMethod.Parameters.Where(x => x.IsOut).Select(x => new Connection(x.Name, this, x.ParameterType)).ToList();

		if (HasReturnValue)
			newConnections.Add(new Connection("Return", this, Graph.SelfMethod.ReturnType));

		Inputs.RemoveRange(1, Inputs.Count - 1);
		Inputs.AddRange(newConnections);

		Graph.Manager.MergeRemovedConnectionsWithNewConnections(newConnections, removedConnections);
	}
}
