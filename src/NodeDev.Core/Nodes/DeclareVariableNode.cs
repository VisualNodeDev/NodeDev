using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using NodeDev.Core.CodeGeneration;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NodeDev.Core.Nodes;

public class DeclareVariableNode : NormalFlowNode
{
	public DeclareVariableNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Declare Variable";

		var t = new UndefinedGenericType("T");
		Outputs.Add(new Connection("Variable", this, t));
		Inputs.Add(new Connection("InitialValue", this, t));
	}

	public override string Name
	{
		get => base.Name;
		set
		{
			base.Name = value;

			if (Outputs.Count > 1) // If not, we're probably still in the constructor
				Outputs[1].Name = value;
		}
	}

	public override string TitleColor => "blue";

	public override bool AllowEditingName => true;

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		return Expression.Assign(info.LocalVariables[Outputs[1]], info.LocalVariables[Inputs[1]]);
	}

	internal override StatementSyntax GenerateRoslynStatement(Dictionary<Connection, Graph.NodePathChunks>? subChunks, GenerationContext context)
	{
		var outputVarName = context.GetVariableName(Outputs[1]);
		var inputVarName = context.GetVariableName(Inputs[1]);

		if (outputVarName == null || inputVarName == null)
			throw new Exception("Variable names not found for DeclareVariableNode");

		return SyntaxHelper.Assignment(
			SyntaxHelper.Identifier(outputVarName),
			SyntaxHelper.Identifier(inputVarName));
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		throw new NotImplementedException();
	}
}
