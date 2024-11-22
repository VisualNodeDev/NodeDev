using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes;

public class Null : NoFlowNode
{
	public Null(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Null";

		Outputs.Add(new("Null", this, new UndefinedGenericType("T")));
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Constant(null, Outputs[0].Type.MakeRealType()); ;
	}
}
