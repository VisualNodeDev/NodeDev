using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;

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
		var left = SyntaxHelper.Identifier(context.GetVariableName(Inputs[0])!);
		var right = SyntaxHelper.Identifier(context.GetVariableName(Inputs[1])!);
		return SyntaxHelper.BinaryExpression(SyntaxKind.SubtractExpression, left, right);
	}
}
