using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;
using System.Linq.Expressions;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes.Math;

public class IsNotNull : BinaryOperationMath
{
	public IsNotNull(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "IsNotNull";

		Outputs[0].UpdateTypeAndTextboxVisibility(TypeFactory.Get<bool>(), false);
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.ReferenceNotEqual(info.LocalVariables[Inputs[0]], Expression.Constant(null));
	}

	internal override ExpressionSyntax GenerateRoslynExpression(GenerationContext context)
	{
		var value = SF.IdentifierName(context.GetVariableName(Inputs[0])!);
		var nullLiteral = SF.LiteralExpression(SyntaxKind.NullLiteralExpression);

		// Generate value != null
		return SF.BinaryExpression(SyntaxKind.NotEqualsExpression, value, nullLiteral);
	}
}
