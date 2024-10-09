using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class Equals : TwoOperationMath
{
	protected override string OperatorName => "Equality";

	public Equals(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Equals";

		Outputs[0].UpdateTypeAndTextboxVisibility(TypeFactory.Get(typeof(bool), null), overrideInitialType: true);
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Equal(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}
}
