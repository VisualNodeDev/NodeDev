using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class Add : TwoOperationMath
{
	protected override string OperatorName => "Addition";

	public Add(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Add";
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Add(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}
}
