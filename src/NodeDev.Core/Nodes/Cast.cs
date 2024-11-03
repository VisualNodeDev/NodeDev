using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes;

public class Cast : NoFlowNode
{
	public Cast(Graph graph, string? id = null) : base(graph, id)
	{
		Inputs.Add(new("Value", this, new UndefinedGenericType("T1")));

		Outputs.Add(new("Result", this, new UndefinedGenericType("T2")));
	}

	public override string Name => "Cast";

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Convert(info.LocalVariables[Inputs[0]], Outputs[0].Type.MakeRealType());
	}
}
