using NodeDev.Core.Connections;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes;

public class Self : NoFlowNode
{
	public Self(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Self";

		Outputs.Add(new("self", this, Project.GetNodeClassType(graph.SelfClass)));
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		if (info.ThisExpression == null)
			throw new Exception("Self node should not be used outside of a non static graph");

		info.LocalVariables[Outputs[0]] = info.ThisExpression;
	}

	protected override void ExecuteInternal(GraphExecutor executor, object? self, Span<object?> inputs, Span<object?> outputs)
	{
		outputs[0] = self;
	}
}
