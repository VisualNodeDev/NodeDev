using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using NodeDev.Core.CodeGeneration;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes.Flow;

public class Branch : FlowNode
{
	public override bool IsFlowNode => true;

	public Branch(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Branch";

		Inputs.Add(new("Exec", this, TypeFactory.ExecType));
		Inputs.Add(new("Condition", this, TypeFactory.Get<bool>()));

		Outputs.Add(new("IfTrue", this, TypeFactory.ExecType));
		Outputs.Add(new("IfFalse", this, TypeFactory.ExecType));
	}

	public override string GetExecOutputPathId(string pathId, Connection execOutput)
	{
		return pathId + "-" + execOutput.Id; // every path is unique
	}

	public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => false;

	public override bool DoesOutputPathAllowMerge(Connection execOutput) => true;

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		ArgumentNullException.ThrowIfNull(subChunks);

		var ifTrue = Graph.BuildExpression(subChunks[Outputs[0]], info);
		var ifFalse = Graph.BuildExpression(subChunks[Outputs[1]], info);

		if (ifTrue.Length == 0 && ifFalse.Length == 0)
			throw new InvalidOperationException("Branch node must have at least a 'IfTrue' of 'IfFalse' statement.");
		else if (ifTrue.Length == 0) // NOT the operation, instead of "if(condition){} else { ...}" we'll have if(!condition) { ... }
			return Expression.IfThen(Expression.Not(info.LocalVariables[Inputs[1]]), Expression.Block(ifFalse));
		else if (ifFalse.Length == 0)
			return Expression.IfThen(info.LocalVariables[Inputs[1]], Expression.Block(ifTrue));
		else
			return Expression.IfThenElse(info.LocalVariables[Inputs[1]], Expression.Block(ifTrue), Expression.Block(ifFalse));
	}

	internal override StatementSyntax GenerateRoslynStatement(Dictionary<Connection, Graph.NodePathChunks>? subChunks, GenerationContext context)
	{
		ArgumentNullException.ThrowIfNull(subChunks);

		// Build the true and false branches
		var builder = new RoslynGraphBuilder(Graph, context);
		var ifTrueStatements = builder.BuildStatements(subChunks[Outputs[0]]);
		var ifFalseStatements = builder.BuildStatements(subChunks[Outputs[1]]);

		if (ifTrueStatements.Count == 0 && ifFalseStatements.Count == 0)
			throw new InvalidOperationException("Branch node must have at least a 'IfTrue' or 'IfFalse' statement.");

		var conditionVarName = context.GetVariableName(Inputs[1]);
		if (conditionVarName == null)
			throw new Exception("Condition variable not found");

		var condition = SF.IdentifierName(conditionVarName);

		// Optimize for empty branches
		if (ifTrueStatements.Count == 0)
		{
			// if (!condition) { ifFalse }
			return SF.IfStatement(
				SF.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, condition),
				SF.Block(ifFalseStatements));
		}
		else if (ifFalseStatements.Count == 0)
		{
			// if (condition) { ifTrue }
			return SF.IfStatement(condition, SF.Block(ifTrueStatements));
		}
		else
		{
			// if (condition) { ifTrue } else { ifFalse }
			return SF.IfStatement(
				condition,
				SF.Block(ifTrueStatements),
				SF.ElseClause(SF.Block(ifFalseStatements)));
		}
	}
}