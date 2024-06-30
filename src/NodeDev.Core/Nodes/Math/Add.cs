using NodeDev.Core.Connections;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class Add : TwoOperationMath
{
	protected override string OperatorName => "Addition";

	public Add(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Add";
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Add(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}

	protected override void ExecuteInternal(GraphExecutor graphExecutor, object? self, Span<object?> inputs, Span<object?> outputs)
	{
		dynamic? a = inputs[0];
		dynamic? b = inputs[1];

		outputs[0] = a + b;
	}
}
