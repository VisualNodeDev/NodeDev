using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class And : NoFlowNode
{
	public And(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "And";

		Inputs.Add(new("a", this, TypeFactory.Get<bool>()));
		Inputs.Add(new("b", this, TypeFactory.Get<bool>()));

		Outputs.Add(new("c", this, TypeFactory.Get<bool>()));
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.And(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}
}
