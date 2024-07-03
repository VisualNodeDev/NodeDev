using NodeDev.Core.Connections;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class Xor : TwoOperationMath
{
	protected override string OperatorName => "ExclusiveOr";
	public Xor(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Xor";
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.ExclusiveOr(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}
}
