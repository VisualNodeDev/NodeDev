using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using NodeDev.Core.CodeGeneration;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes.Flow;

public class TryCatchNode : FlowNode
{
	public override bool IsFlowNode => true;

	public TryCatchNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "TryCatch";

		Inputs.Add(new("Exec", this, TypeFactory.ExecType));

		Outputs.Add(new("Try", this, TypeFactory.ExecType));
		Outputs.Add(new("Catch", this, TypeFactory.ExecType));
		Outputs.Add(new("Finally", this, TypeFactory.ExecType));
		Outputs.Add(new("Exception", this, new UndefinedGenericType("T"), linkedExec: Outputs[1]));
	}

	public override string GetExecOutputPathId(string pathId, Connection execOutput)
	{
		return pathId + "-" + execOutput.Id;
	}

	// We're allowed to have 'nothing' in the blocks, ex an empty "finally" or an empty "catch"
	public override bool DoesOutputPathAllowDeadEnd(Connection execOutput)
	{
		return execOutput.Connections.Count == 0;
	}

	/// <summary>
	/// We allow merging back together at the end. Technically all paths could continue, such as :
	/// Try { } catch( Exception ex) {} finally { } ...
	/// </summary>
	public override bool DoesOutputPathAllowMerge(Connection execOutput) => true;

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		ArgumentNullException.ThrowIfNull(subChunks);

		var tryBlock = Expression.Block(Graph.BuildExpression(subChunks[Outputs[0]], info));
		var catchBlock = Expression.Block(Graph.BuildExpression(subChunks[Outputs[1]], info));
		var finallyBlock = Expression.Block(Graph.BuildExpression(subChunks[Outputs[2]], info));

		//var exceptionVariable = Expression.Variable(Outputs[3].Type.MakeRealType(), "ex");
		//info.LocalVariables[Outputs[3]] = exceptionVariable; // Make sure other pieces of code use the right variable for that exception

		var catchClause = Expression.Catch(Outputs[3].Type.MakeRealType(), catchBlock);

		return Expression.Block(Expression.TryCatchFinally(tryBlock, Outputs[2].Connections.Count == 0 ? null : finallyBlock, catchClause));
	}

	internal override StatementSyntax GenerateRoslynStatement(Dictionary<Connection, Graph.NodePathChunks>? subChunks, GenerationContext context)
	{
		ArgumentNullException.ThrowIfNull(subChunks);

		var builder = new RoslynGraphBuilder(Graph, context);

		// Build try block
		var tryStatements = builder.BuildStatements(subChunks[Outputs[0]]);
		var tryBlock = SF.Block(tryStatements);

		// Build catch block - register exception variable first
		var exceptionVarName = context.GetUniqueName("ex");
		context.RegisterVariableName(Outputs[3], exceptionVarName);
		
		var catchStatements = builder.BuildStatements(subChunks[Outputs[1]]);
		
		// Create exception variable for catch clause
		var exceptionType = RoslynHelpers.GetTypeSyntax(Outputs[3].Type);
		
		var catchDeclaration = SF.CatchDeclaration(exceptionType)
			.WithIdentifier(SF.Identifier(exceptionVarName));
		
		var catchClause = SF.CatchClause()
			.WithDeclaration(catchDeclaration)
			.WithBlock(SF.Block(catchStatements));

		// Build finally block (if it has connections)
		FinallyClauseSyntax? finallyClause = null;
		if (Outputs[2].Connections.Count > 0)
		{
			var finallyStatements = builder.BuildStatements(subChunks[Outputs[2]]);
			finallyClause = SF.FinallyClause(SF.Block(finallyStatements));
		}

		// Create try statement
		var tryStatement = SF.TryStatement()
			.WithBlock(tryBlock)
			.WithCatches(SF.SingletonList(catchClause));

		if (finallyClause != null)
		{
			tryStatement = tryStatement.WithFinally(finallyClause);
		}

		return tryStatement;
	}
}
