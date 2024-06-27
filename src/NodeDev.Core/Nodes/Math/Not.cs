using NodeDev.Core.Connections;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class Not : BinaryOperationMath
{
	public Not(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Not";
	}

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		if (subChunks != null)
			throw new Exception("TwoOperationMath nodes should not have subchunks");

		return Expression.Assign(info.LocalVariables[Outputs[0]], Expression.Not(info.LocalVariables[Inputs[0]]));
	}

	protected override void ExecuteInternal(GraphExecutor graphExecutor, object? self, Span<object?> inputs, Span<object?> outputs)
	{
		dynamic? a = inputs[0];

		outputs[0] = !a;
	}
}
