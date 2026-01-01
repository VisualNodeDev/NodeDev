using NodeDev.Core.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

	internal override ExpressionSyntax GenerateRoslynExpression(GenerationContext context)
	{
		// In non-static methods, "this" refers to the current instance
		// For static methods, this should never be called
		return SF.ThisExpression();
	}
}
