using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class Not : BinaryOperationMath
{
	public Not(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Not";
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Not(info.LocalVariables[Inputs[0]]);
	}
}
