using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes.Math;

public class Not : BinaryOperationMath
{
	public Not(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Not";
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Not(info.LocalVariables[Inputs[0]]);
	}

	internal override ExpressionSyntax GenerateRoslynExpression(GenerationContext context)
	{
		var operand = SF.IdentifierName(context.GetVariableName(Inputs[0])!);
		return SF.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, operand);
	}
}
