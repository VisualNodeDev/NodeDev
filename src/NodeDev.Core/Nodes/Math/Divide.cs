using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class Divide : TwoOperationMath
{
	protected override string OperatorName => "Division";
	public Divide(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Divide";
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Divide(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}
}
