using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class Or : NoFlowNode
{
	public Or(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Or";

		Inputs.Add(new("a", this, TypeFactory.Get<bool>()));
		Inputs.Add(new("b", this, TypeFactory.Get<bool>()));

		Outputs.Add(new("c", this, TypeFactory.Get<bool>()));
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Or(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}
}
