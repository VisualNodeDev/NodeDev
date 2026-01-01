using NodeDev.Core.Types;
using NodeDev.Core.CodeGeneration;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

	internal override ExpressionSyntax GenerateRoslynExpression(GenerationContext context)
	{
		var value = SF.IdentifierName(context.GetVariableName(Inputs[0])!);
		var targetType = RoslynHelpers.GetTypeSyntax(Outputs[0].Type);
		
		// Generate (TargetType)value
		return SF.CastExpression(targetType, value);
	}
}
