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

		Outputs[0].UpdateType(TypeFactory.Get(typeof(bool), null));
	}

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		if (subChunks != null)
			throw new Exception("TwoOperationMath nodes should not have subchunks");

		return Expression.Assign(info.LocalVariables[Outputs[0]], Expression.NotEqual(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]));
	}

	protected override void ExecuteInternal(GraphExecutor graphExecutor, object? self, Span<object?> inputs, Span<object?> outputs)
	{
		dynamic? a = inputs[0];
		dynamic? b = inputs[1];

		outputs[0] = a != b;
	}
}
