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

	protected override void ExecuteInternal(GraphExecutor graphExecutor, object? self, Span<object?> inputs, Span<object?> outputs)
	{
		if (inputs[0] == null || inputs[1] == null)
		{
			outputs[0] = null;
			return;
		}

		var a = (bool)inputs[0]!;
		var b = (bool)inputs[1]!;

		outputs[0] = a && b;
	}
}
