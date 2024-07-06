using NodeDev.Core.Connections;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class Subtract : TwoOperationMath
{
	protected override string OperatorName => "Subtraction";
	public Subtract(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Subtract";
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Subtract(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}
}
