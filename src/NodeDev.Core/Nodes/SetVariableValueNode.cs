using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using NodeDev.Core.CodeGeneration;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes;

public class SetVariableValueNode : NormalFlowNode
{
	public SetVariableValueNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Set Variable";

		var type = new UndefinedGenericType("T");
		Inputs.Add(new Connection("Variable", this, type));
		Inputs.Add(new Connection("Value", this, type));
	}

	public override string Name
	{
		get
		{
			if (Inputs.Count > 1)
			{
				var variableNode = Inputs[1].Connections.FirstOrDefault()?.Parent;
				if (variableNode != null)
					return $"Set {variableNode.Name}";
			}

			return "Set Variable";
		}

		set { }
	}

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		return Expression.Assign(info.LocalVariables[Inputs[1]], info.LocalVariables[Inputs[2]]);
	}

	internal override StatementSyntax GenerateRoslynStatement(Dictionary<Connection, Graph.NodePathChunks>? subChunks, GenerationContext context)
	{
		var variableVarName = context.GetVariableName(Inputs[1]);
		var valueVarName = context.GetVariableName(Inputs[2]);

		if (variableVarName == null || valueVarName == null)
			throw new Exception("Variable names not found for SetVariableValueNode");

		// Generate variable = value;
		return SF.ExpressionStatement(
			SF.AssignmentExpression(
				SyntaxKind.SimpleAssignmentExpression,
				SF.IdentifierName(variableVarName),
				SF.IdentifierName(valueVarName)));
	}
}
