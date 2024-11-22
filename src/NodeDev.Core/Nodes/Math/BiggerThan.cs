using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class BiggerThan : NoFlowNode
{
	public BiggerThan(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "BiggerThan";

		Inputs.Add(new("a", this, new UndefinedGenericType("T1")));
		Inputs.Add(new("b", this, new UndefinedGenericType("T2")));

		Outputs.Add(new("c", this, TypeFactory.Get<bool>()));
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.GreaterThan(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}
}
