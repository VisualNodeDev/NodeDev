using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;

namespace NodeDev.Core.Nodes.Math;

public class Modulo : TwoOperationMath
{
	protected override string OperatorName => "Modulus";
	public Modulo(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Modulo";
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Modulo(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}

	internal override ExpressionSyntax GenerateRoslynExpression(GenerationContext context)
	{
		var left = SyntaxHelper.Identifier(context.GetVariableName(Inputs[0])!);
		var right = SyntaxHelper.Identifier(context.GetVariableName(Inputs[1])!);
		return SyntaxHelper.BinaryExpression(SyntaxKind.ModuloExpression, left, right);
	}
}
