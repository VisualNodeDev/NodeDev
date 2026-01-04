using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;
using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes.Debug;

public class Sleep : NormalFlowNode
{
	public Sleep(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Sleep";
		Inputs.Add(new("TimeMilliseconds", this, TypeFactory.Get<int>()));
	}

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		throw new NotImplementedException();
	}

	internal override StatementSyntax GenerateRoslynStatement(Dictionary<Connection, Graph.NodePathChunks>? subChunks, GenerationContext context)
	{
		if (subChunks != null)
			throw new Exception("WriteLine node should not have subchunks");

		var value = SF.IdentifierName(context.GetVariableName(Inputs[1])!);

		// Generate Console.WriteLine(value)
		var memberAccess = SF.MemberAccessExpression(
			SyntaxKind.SimpleMemberAccessExpression,
			SF.IdentifierName("Thread"),
			SF.IdentifierName("Sleep"));

		var invocation = SF.InvocationExpression(memberAccess)
			.WithArgumentList(SF.ArgumentList(SF.SingletonSeparatedList(SF.Argument(value))));

		return SF.ExpressionStatement(invocation);
	}
}
