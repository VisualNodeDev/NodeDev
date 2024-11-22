using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class Modulo : TwoOperationMath
{
	protected override string OperatorName => "Modulus";
	public Modulo(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Modulo";
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Modulo(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}
}
