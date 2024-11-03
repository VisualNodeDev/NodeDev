using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Flow;

public class ThrowNode : FlowNode
{
    public override bool IsFlowNode => true;

    public override bool BreaksDeadEnd => true;

    public ThrowNode(Graph graph, string? id = null) : base(graph, id)
    {
        Name = "Throw";

        Inputs.Add(new("Exec", this, TypeFactory.ExecType));
        Inputs.Add(new("Exception", this, TypeFactory.Get<Exception>()));
    }

    internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
    {
        return Expression.Throw(info.LocalVariables[Inputs[1]]);
    }
}
