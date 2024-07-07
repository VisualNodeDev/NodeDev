using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Flow;

public class WhileNode : FlowNode
{

	public override bool IsFlowNode => true;

	public override bool AllowRemergingExecConnections => false;

	public WhileNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "While";

		Inputs.Add(new("Exec", this, TypeFactory.ExecType));
		Inputs.Add(new("Condition", this, TypeFactory.Get<bool>()));

		Outputs.Add(new("ExecLoop", this, TypeFactory.ExecType));
		Outputs.Add(new("ExecOut", this, TypeFactory.ExecType));
	}

	public override string GetExecOutputPathId(string pathId, Connection execOutput)
	{
		if (execOutput == Outputs[0])
			return pathId + "-" + execOutput.Id;
		else
			return pathId;
	}

	public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => execOutput == Outputs[0]; // Loop has to be a dead end

	public override bool DoesOutputPathAllowMerge(Connection execOutput) => execOutput == Outputs[1]; // Only the ExecOut path can merge. The loop path can never merge and always ends in a dead end.

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
    {
		ArgumentNullException.ThrowIfNull(subChunks);

		var loopBody = Expression.Block(Graph.BuildExpression(subChunks[Outputs[0]], info)); // Build the loop body
		var afterLoop = Expression.Block(Graph.BuildExpression(subChunks[Outputs[1]], info)); // Build the after loop body

        var breakLabel = Expression.Label();
        var loop = Expression.Loop(
            Expression.IfThenElse(
				info.LocalVariables[Inputs[1]], // while condition
                loopBody, // does the assign for enumerator.Current, as well as the loop body
                Expression.Break(breakLabel) // break the loop
            ),
            breakLabel
        );

		return Expression.Block(loop, afterLoop);
    }
}
