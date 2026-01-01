using NodeDev.Core.Types;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes.Math;

public class BiggerThan : NoFlowNode
{
	public BiggerThan(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "BiggerThan";

		Inputs.Add(new("a", this, new UndefinedGenericType("T1")));
		Inputs.Add(new("b", this, new UndefinedGenericType("T2")));

		Outputs.Add(new("c", this, TypeFactory.Get<bool>()));
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.GreaterThan(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
	}

	internal override ExpressionSyntax GenerateRoslynExpression(GenerationContext context)
	{
		var left = SF.IdentifierName(context.GetVariableName(Inputs[0])!);
		var right = SF.IdentifierName(context.GetVariableName(Inputs[1])!);
		return SF.BinaryExpression(SyntaxKind.GreaterThanExpression, left, right);
	}
}
