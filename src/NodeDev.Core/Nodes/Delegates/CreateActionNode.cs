namespace NodeDev.Core.Nodes.Delegates;

public sealed class CreateActionNode : CreateDelegateNode
{
	public CreateActionNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Create Action";
		InitializeSignature(resultType: null);
	}

	public override DelegateKind Kind => DelegateKind.Action;
}
