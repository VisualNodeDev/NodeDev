using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;
using NodeDev.Core.Types;
using System.Linq.Expressions;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes.Math;

public class Equals : TwoOperationMath
{
	protected override string OperatorName => "Equality";

	public Equals(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Equals";

		Outputs[0].UpdateTypeAndTextboxVisibility(TypeFactory.Get(typeof(bool), null), overrideInitialType: true);
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Equal(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}

	internal override ExpressionSyntax GenerateRoslynExpression(GenerationContext context)
	{
		var left = SF.IdentifierName(context.GetVariableName(Inputs[0])!);
		var right = SF.IdentifierName(context.GetVariableName(Inputs[1])!);
		return SF.BinaryExpression(SyntaxKind.EqualsExpression, left, right);
	}
}
