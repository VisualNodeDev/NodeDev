using NodeDev.Core.Connections;

namespace NodeDev.Core.Nodes.Delegates;

public sealed class LambdaEntryNode : Flow.FlowNode
{
	public LambdaEntryNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Lambda Entry";
		Outputs.Add(new Connection("Exec", this, TypeFactory.ExecType));
	}

	public override string TitleColor => "red";
	public override bool IsFlowNode => true;
	public Connection ExecOutput => Outputs[0];
	public IReadOnlyList<Connection> ParameterOutputs
	{
		get
		{
			var owner = GetOwner();
			return Outputs.Skip(1).Take(owner.Parameters.Count).ToList();
		}
	}
	public IReadOnlyList<Connection> CaptureOutputs
	{
		get
		{
			var owner = GetOwner();
			return Outputs.Skip(1 + owner.Parameters.Count).Take(owner.Captures.Count).ToList();
		}
	}

	internal void RefreshFromOwner(CreateDelegateNode owner)
	{
		if (CallableScopeId != owner.BodyScopeId)
			throw new InvalidOperationException("Lambda entry does not belong to the supplied delegate scope.");

		var desired = new List<(string Name, Types.TypeBase Type)> { ("Exec", TypeFactory.ExecType) };
		desired.AddRange(owner.Parameters.Select(x => (x.Name, x.Type)));
		desired.AddRange(owner.Captures.Select(x => ($"Captured {x.Name}", x.Type)));
		CreateDelegateNode.ReconcileConnections(this, Outputs, desired);
	}

	private CreateDelegateNode GetOwner()
	{
		return Graph.GetOwningLambda(CallableScopeId)
			?? throw new InvalidOperationException("Lambda entry has no owning delegate.");
	}

	internal override void FinalizeDeserialization()
	{
		var owner = Graph.GetOwningLambda(CallableScopeId);
		if (owner != null)
			RefreshFromOwner(owner);
	}

	public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => false;
	public override bool DoesOutputPathAllowMerge(Connection execOutput) => throw new NotSupportedException();
	public override string GetExecOutputPathId(string pathId, Connection execOutput) => throw new NotSupportedException();
}
