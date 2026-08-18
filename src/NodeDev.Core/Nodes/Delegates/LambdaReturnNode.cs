using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;
using NodeDev.Core.Connections;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes.Delegates;

public sealed class LambdaReturnNode : Flow.FlowNode
{
	public LambdaReturnNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Lambda Return";
		Inputs.Add(new Connection("Exec", this, TypeFactory.ExecType));
		Inputs.Add(new Connection("Result", this, new Types.UndefinedGenericType($"LambdaReturn_{Id.Replace('-', '_')}")));
	}

	public override string TitleColor => "red";
	public override bool IsFlowNode => true;
	public override bool BreaksDeadEnd => true;
	public Connection ExecInput => Inputs[0];
	public Connection ResultInput => Inputs[1];

	internal void RefreshFromOwner(CreateDelegateNode owner)
	{
		if (owner.Kind != DelegateKind.Func || owner.ResultType == null)
			throw new InvalidOperationException("Lambda return nodes are valid only in Func scopes.");
		if (CallableScopeId != owner.BodyScopeId)
			throw new InvalidOperationException("Lambda return does not belong to the supplied delegate scope.");
		CreateDelegateNode.ReconcileConnections(this, Inputs, [("Exec", TypeFactory.ExecType), ("Result", owner.ResultType)]);
	}

	internal override void FinalizeDeserialization()
	{
		var owner = Graph.GetOwningLambda(CallableScopeId);
		if (owner?.Kind == DelegateKind.Func)
			RefreshFromOwner(owner);
	}

	internal override StatementSyntax GenerateRoslynStatement(Dictionary<Connection, Graph.NodePathChunks>? subChunks, GenerationContext context)
	{
		var resultName = context.GetVariableName(ResultInput)
			?? throw new InvalidOperationException("Lambda result input was not resolved.");
		return SF.ReturnStatement(SF.IdentifierName(resultName));
	}

	public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => true;
	public override bool DoesOutputPathAllowMerge(Connection execOutput) => throw new NotSupportedException();
	public override string GetExecOutputPathId(string pathId, Connection execOutput) => throw new NotSupportedException();
}
