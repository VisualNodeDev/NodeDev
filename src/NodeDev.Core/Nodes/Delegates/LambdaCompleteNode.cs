using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;
using NodeDev.Core.Connections;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes.Delegates;

public sealed class LambdaCompleteNode : Flow.FlowNode
{
	public LambdaCompleteNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Lambda Complete";
		Inputs.Add(new Connection("Exec", this, TypeFactory.ExecType));
	}

	public override string TitleColor => "red";
	public override bool IsFlowNode => true;
	public override bool BreaksDeadEnd => true;
	public Connection ExecInput => Inputs[0];

	internal override StatementSyntax GenerateRoslynStatement(Dictionary<Connection, Graph.NodePathChunks>? subChunks, GenerationContext context)
	{
		return SF.ReturnStatement();
	}

	public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => true;
	public override bool DoesOutputPathAllowMerge(Connection execOutput) => throw new NotSupportedException();
	public override string GetExecOutputPathId(string pathId, Connection execOutput) => throw new NotSupportedException();
}
