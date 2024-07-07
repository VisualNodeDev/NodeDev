using FastExpressionCompiler;
using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Flow;

public class ForNode : FlowNode
{
    public override bool IsFlowNode => true;

    public override bool FetchState => true;

    public override bool ReOrderExecInputsAndOutputs => false;

    public override bool AllowRemergingExecConnections => false;

    public ForNode(Graph graph, string? id = null) : base(graph, id)
    {
        Name = "For";

        Inputs.Add(new("Exec", this, TypeFactory.ExecType));
        Inputs.Add(new("Start", this, TypeFactory.Get<int>()));
        Inputs.Add(new("End (Exclude)", this, TypeFactory.Get<int>()));

        Outputs.Add(new("ExecLoop", this, TypeFactory.ExecType));
        Outputs.Add(new("Index", this, TypeFactory.Get<int>(), linkedExec: Outputs[0]));
        Outputs.Add(new("ExecOut", this, TypeFactory.ExecType));
    }

    public override string GetExecOutputPathId(string pathId, Connection execOutput)
    {
        if (execOutput == Outputs[0])
            return pathId + "-" + execOutput.Id;
        else if (execOutput == Outputs[2])
            return pathId;
        else
            throw new Exception("Invalid exec output");
    }

    public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => execOutput == Outputs[0]; // The loop exec path must be a dead end (or a breaking node, such as return, continue, break)

    public override bool DoesOutputPathAllowMerge(Connection execOutput) => execOutput == Outputs[2]; // the ExecOut path allows merging, but not the loop. The loop is always a dead end.

    internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
    {
        ArgumentNullException.ThrowIfNull(subChunks);

        var count = info.LocalVariables[Outputs[1]];
        var assignCount = Expression.Assign(count, info.LocalVariables[Inputs[1]]); // assign the start value to the count variable
        var incrementCount = Expression.PreIncrementAssign(count); // increment the count variable

        var loopBody = Expression.Block(Graph.BuildExpression(subChunks[Outputs[0]], info).Append(incrementCount)); // Build the loop body and the counter increment
        var afterLoop = Expression.Block(Graph.BuildExpression(subChunks[Outputs[2]], info)); // Build the after loop body

        var breakLabel = Expression.Label();
        var loop = Expression.Loop(
            Expression.IfThenElse(
                Expression.LessThan(count, info.LocalVariables[Inputs[2]]), // i < end
                loopBody, // does the assign for enumerator.Current, as well as the loop body
                Expression.Break(breakLabel) // break the loop
            ),
            breakLabel
        );

        return Expression.Block(assignCount, loop, afterLoop);
    }
}
