using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;
using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes.Delegates;

public sealed class InvokeDelegateNode : NormalFlowNode
{
	public InvokeDelegateNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Invoke Delegate";
		Inputs.Add(new Connection("Delegate", this, new UndefinedGenericType($"Delegate_{Id.Replace('-', '_')}")));
	}

	public Connection DelegateInput => Inputs[1];
	public IReadOnlyList<Connection> InvocationInputs => Inputs.Skip(2).ToList();
	public Connection? ResultOutput => Outputs.Skip(1).SingleOrDefault();

	internal void InitializeFromDelegateType(TypeBase delegateType)
	{
		if (!BclDelegateType.TryDescribe(delegateType, out _, out var parameterTypes, out var resultType))
			throw new ArgumentException($"{delegateType.FriendlyName} is not a supported Action or Func delegate.", nameof(delegateType));

		var desiredInputs = new List<(string Name, TypeBase Type)>
		{
			("Exec", TypeFactory.ExecType),
			("Delegate", delegateType)
		};
		for (var index = 0; index < parameterTypes.Count; index++)
			desiredInputs.Add(($"Argument {index + 1}", parameterTypes[index]));
		CreateDelegateNode.ReconcileConnections(this, Inputs, desiredInputs);

		var desiredOutputs = new List<(string Name, TypeBase Type)> { ("Exec", TypeFactory.ExecType) };
		if (resultType != null)
			desiredOutputs.Add(("Result", resultType));
		CreateDelegateNode.ReconcileConnections(this, Outputs, desiredOutputs);
		Graph.RaiseGraphChanged(true);
	}

	public override List<Connection> GenericConnectionTypeDefined(Connection connection)
	{
		if (connection == DelegateInput && BclDelegateType.TryDescribe(connection.Type, out _, out _, out _))
		{
			InitializeFromDelegateType(connection.Type);
			return InputsAndOutputs.ToList();
		}
		return [];
	}

	internal override void FinalizeDeserialization()
	{
		if (Inputs.Count > 1 && BclDelegateType.TryDescribe(Inputs[1].Type, out _, out _, out _))
			InitializeFromDelegateType(Inputs[1].Type);
	}

	internal override StatementSyntax GenerateRoslynStatement(Dictionary<Connection, Graph.NodePathChunks>? subChunks, GenerationContext context)
	{
		if (!BclDelegateType.TryDescribe(DelegateInput.Type, out _, out _, out var resultType))
			throw new InvalidOperationException("Delegate input must resolve to a supported Action or Func type before generation.");

		var delegateName = context.GetVariableName(DelegateInput)
			?? throw new InvalidOperationException("Delegate input was not resolved.");
		var arguments = InvocationInputs.Select(input =>
		{
			var name = context.GetVariableName(input)
				?? throw new InvalidOperationException($"Delegate argument {input.Name} was not resolved.");
			return SF.Argument(SF.IdentifierName(name));
		});
		var invocation = SF.InvocationExpression(SF.IdentifierName(delegateName))
			.WithArgumentList(SF.ArgumentList(SF.SeparatedList(arguments)));

		if (resultType == null)
			return SF.ExpressionStatement(invocation);

		var resultName = ResultOutput == null ? null : context.GetVariableName(ResultOutput);
		if (resultName == null)
			throw new InvalidOperationException("Delegate result output was not declared.");
		return SF.ExpressionStatement(SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SF.IdentifierName(resultName), invocation));
	}
}
