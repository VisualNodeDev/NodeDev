using NodeDev.Core.Connections;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class IsNotNull : BinaryOperationMath
{
	public IsNotNull(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "IsNotNull";

		Outputs[0].UpdateTypeAndTextboxVisibility(TypeFactory.Get<bool>(), false);
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.ReferenceNotEqual(info.LocalVariables[Inputs[0]], Expression.Constant(null));
	}
}
