using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Math;

public class SmallerThanOrEqual : NoFlowNode
{
	public SmallerThanOrEqual(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "SmallerThanOrEqual";

		Inputs.Add(new("a", this, new UndefinedGenericType("T1")));
		Inputs.Add(new("b", this, new UndefinedGenericType("T2")));

		Outputs.Add(new("c", this, TypeFactory.Get<bool>()));
	}

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		if (subChunks != null)
			throw new Exception("TwoOperationMath nodes should not have subchunks");

		return Expression.Assign(info.LocalVariables[Outputs[0]], Expression.LessThanOrEqual(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]));
	}

	protected override void ExecuteInternal(GraphExecutor graphExecutor, object? self, Span<object?> inputs, Span<object?> outputs)
	{
		dynamic? a = inputs[0];
		dynamic? b = inputs[1];

		outputs[0] = a <= b;
	}
}
