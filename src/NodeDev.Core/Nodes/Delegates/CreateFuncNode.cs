namespace NodeDev.Core.Nodes.Delegates;

public sealed class CreateFuncNode : CreateDelegateNode
{
	public CreateFuncNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Create Func";
		InitializeSignature(TypeFactory.Get<bool>());
	}

	public override DelegateKind Kind => DelegateKind.Func;
}
