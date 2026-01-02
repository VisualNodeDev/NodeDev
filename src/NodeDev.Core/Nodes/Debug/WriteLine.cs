using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;
using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes.Debug;

public class WriteLine : NormalFlowNode
{
	public WriteLine(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "WriteLine";

		Inputs.Add(new("Line", this, new UndefinedGenericType("T")));
	}

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		if (subChunks != null)
			throw new Exception("WriteLine node should not have subchunks");

		var method = typeof(Console).GetMethod(nameof(Console.WriteLine), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, [typeof(object)]);
		if (method == null)
			throw new Exception("Unable to find Console.WriteLine method");

		return Expression.Call(null, method, Expression.Convert(info.LocalVariables[Inputs[1]], typeof(object)));
	}

	internal override StatementSyntax GenerateRoslynStatement(Dictionary<Connection, Graph.NodePathChunks>? subChunks, GenerationContext context)
	{
		if (subChunks != null)
			throw new Exception("WriteLine node should not have subchunks");

		var value = SF.IdentifierName(context.GetVariableName(Inputs[1])!);

		// Generate Console.WriteLine(value)
		var memberAccess = SF.MemberAccessExpression(
			SyntaxKind.SimpleMemberAccessExpression,
			SF.IdentifierName("Console"),
			SF.IdentifierName("WriteLine"));

		var invocation = SF.InvocationExpression(memberAccess)
			.WithArgumentList(SF.ArgumentList(SF.SingletonSeparatedList(SF.Argument(value))));

		return SF.ExpressionStatement(invocation);
	}
}
