using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Flow;

public class TryCatchNode : FlowNode
{
    public override bool IsFlowNode => true;

    public TryCatchNode(Graph graph, string? id = null) : base(graph, id)
    {
        Name = "TryCatch";

        Inputs.Add(new("Exec", this, TypeFactory.ExecType));

        Outputs.Add(new("Try", this, TypeFactory.ExecType));
        Outputs.Add(new("Catch", this, TypeFactory.ExecType));
        Outputs.Add(new("Finally", this, TypeFactory.ExecType));
        Outputs.Add(new("Exception", this, TypeFactory.Get<Exception>(), linkedExec: Outputs[1]));
    }

    public override string GetExecOutputPathId(string pathId, Connection execOutput)
    {
        return pathId + "-" + execOutput.Id;
    }

    public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => false;

    public override bool DoesOutputPathAllowMerge(Connection execOutput) => true;

    internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
    {
        ArgumentNullException.ThrowIfNull(subChunks);

        var tryBlock = Expression.Block(Graph.BuildExpression(subChunks[Outputs[0]], info));
        var catchBlock = Expression.Block(Graph.BuildExpression(subChunks[Outputs[1]], info));
        var finallyBlock = Expression.Block(Graph.BuildExpression(subChunks[Outputs[2]], info));

        var exceptionVariable = Expression.Variable(typeof(Exception), "ex");
        info.LocalVariables[Outputs[3]] = exceptionVariable; // Make sure other pieces of code use the right variable for that exception

        var catchClause = Expression.Catch(exceptionVariable, catchBlock);

        return Expression.TryCatchFinally(tryBlock, finallyBlock, catchClause);
    }
}
