using NodeDev.Core.Types;

namespace NodeDev.Core.Nodes.Delegates;

public sealed class CreateFuncNode : CreateDelegateNode
{
	public CreateFuncNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Create Func";
		InitializeSignature(new UndefinedGenericType($"LambdaResult_{Id.Replace('-', '_')}"));
	}

	public override DelegateKind Kind => DelegateKind.Func;
}
