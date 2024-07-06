using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Flow;

public class Branch : FlowNode
{
	public override bool IsFlowNode => true;

	public Branch(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Branch";

		Inputs.Add(new("Exec", this, TypeFactory.ExecType));
		Inputs.Add(new("Condition", this, TypeFactory.Get<bool>()));

		Outputs.Add(new("IfTrue", this, TypeFactory.ExecType));
		Outputs.Add(new("IfFalse", this, TypeFactory.ExecType));
	}

	public override string GetExecOutputPathId(string pathId, Connection execOutput)
	{
		return pathId + "-" + execOutput.Id; // every path is unique
	}

	public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => false;

	public override bool DoesOutputPathAllowMerge(Connection execOutput) => true;

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		ArgumentNullException.ThrowIfNull(subChunks);

		var ifTrue = Graph.BuildExpression(subChunks[Outputs[0]], info);
		var ifFalse = Graph.BuildExpression(subChunks[Outputs[1]], info);

		var ifThenElse = Expression.IfThenElse(info.LocalVariables[Inputs[1]], Expression.Block(ifTrue), Expression.Block(ifFalse));

		return ifThenElse;
	}
}
