using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes;

public class SetVariableValueNode : NormalFlowNode
{
    public SetVariableValueNode(Graph graph, string? id = null) : base(graph, id)
    {
        Name = "Set Variable";

        var type = new UndefinedGenericType("T");
        Inputs.Add(new Connection("Variable", this, type));
        Inputs.Add(new Connection("Value", this, type));
    }

    internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
    {
        return Expression.Assign(info.LocalVariables[Inputs[1]], info.LocalVariables[Inputs[2]]);
    }
}
