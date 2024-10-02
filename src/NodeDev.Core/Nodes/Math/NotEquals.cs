using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class NotEquals : TwoOperationMath
{
	protected override string OperatorName => "Inequality";
	public NotEquals(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "NotEquals";

		Outputs[0].UpdateTypeAndTextboxVisibility(TypeFactory.Get(typeof(bool), null));
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.NotEqual(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}
}
