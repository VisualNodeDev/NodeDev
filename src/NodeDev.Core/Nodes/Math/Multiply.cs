using NodeDev.Core.Connections;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class Multiply : TwoOperationMath
{
	protected override string OperatorName => "Multiply";
	public Multiply(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Multiply";
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Multiply(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}
}
