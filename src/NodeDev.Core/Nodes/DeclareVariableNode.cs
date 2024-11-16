using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes;

public class DeclareVariableNode : NormalFlowNode
{
    public DeclareVariableNode(Graph graph, string name, string? id = null) : base(graph, id)
    {
        Name = name;

        var t = new UndefinedGenericType("T");
        Outputs.Add(new Connection("Variable", this, t));
        Inputs.Add(new Connection("InitialValue", this, t));
    }

    public override string TitleColor => "blue";

    internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
    {
        return Expression.Assign(info.LocalVariables[Outputs[1]], info.LocalVariables[Inputs[1]]);
    }

    internal override void BuildInlineExpression(BuildExpressionInfo info)
    {
        throw new NotImplementedException();
    }
}
