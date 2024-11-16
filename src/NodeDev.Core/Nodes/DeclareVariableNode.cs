using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes;

public class DeclareVariableNode : FlowNode
{
    public DeclareVariableNode(Graph graph, string name, TypeBase type, string? id = null) : base(graph, id)
    {
        Name = name;
        VariableType = type;

        var type = new UndefinedGenericType("T");
        Outputs.Add(new Connection("Variable", this, type));
        Inputs.Add(new Connection("InitialValue", this, type));
    }

    public override string TitleColor => "blue";

    public override bool IsFlowNode => true;

    internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
    {
        return Expression.Assign(variable, info.LocalVariables[Inputs[0]]);
    }

    internal override void BuildInlineExpression(BuildExpressionInfo info)
    {
        throw new NotImplementedException();
    }
}
