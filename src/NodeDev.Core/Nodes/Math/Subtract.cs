using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes.Math;

public class Subtract : TwoOperationMath
{
	protected override string OperatorName => "Subtraction";
	public Subtract(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Subtract";
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Subtract(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}

	internal override ExpressionSyntax GenerateRoslynExpression(GenerationContext context)
	{
		var left = SF.IdentifierName(context.GetVariableName(Inputs[0])!);
		var right = SF.IdentifierName(context.GetVariableName(Inputs[1])!);
		return SF.BinaryExpression(SyntaxKind.SubtractExpression, left, right);
	}
}
