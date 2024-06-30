using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class SmallerThan : NoFlowNode
{
	public SmallerThan(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "SmallerThan";

		Inputs.Add(new("a", this, new UndefinedGenericType("T1")));
		Inputs.Add(new("b", this, new UndefinedGenericType("T2")));

		Outputs.Add(new("c", this, TypeFactory.Get<bool>()));
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.LessThan(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}

	protected override void ExecuteInternal(GraphExecutor graphExecutor, object? self, Span<object?> inputs, Span<object?> outputs)
	{
		dynamic? a = inputs[0];
		dynamic? b = inputs[1];

		outputs[0] = a < b;
	}
}
