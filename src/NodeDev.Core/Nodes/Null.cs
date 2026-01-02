using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;
using NodeDev.Core.Types;
using System.Linq.Expressions;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes;

public class Null : NoFlowNode
{
	public Null(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Null";

		Outputs.Add(new("Null", this, new UndefinedGenericType("T")));
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Constant(null, Outputs[0].Type.MakeRealType()); ;
	}

	internal override ExpressionSyntax GenerateRoslynExpression(GenerationContext context)
	{
		// Generate null literal
		return SF.LiteralExpression(SyntaxKind.NullLiteralExpression);
	}
}
