using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Flow;

public class ForeachNode : FlowNode
{
	public override bool IsFlowNode => true;

	public override bool FetchState => true;

	public override bool ReOrderExecInputsAndOutputs => false;

	public override bool AllowRemergingExecConnections => false;

	public ForeachNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "Foreach";

		var t = new UndefinedGenericType("T");

		Inputs.Add(new("Exec", this, TypeFactory.ExecType));
		Inputs.Add(new("IEnumerable", this, TypeFactory.Get(typeof(IEnumerable<>), [t])));

		Outputs.Add(new("ExecLoop", this, TypeFactory.ExecType));
		Outputs.Add(new("Item", this, t, linkedExec: Outputs[0]));
		Outputs.Add(new("ExecOut", this, TypeFactory.ExecType));
	}

	public override string GetExecOutputPathId(string pathId, Connection execOutput)
	{
		if (execOutput == Outputs[0])
		{
			return pathId + "-" + execOutput.Id;
		}
		else if (execOutput == Outputs[2])
			return pathId;

		throw new Exception("Unable to find execOutput");
	}

	public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => execOutput == Outputs[0]; // The loop exec path must be a dead end (or a breaking node, such as return, continue, break)

	public override bool DoesOutputPathAllowMerge(Connection execOutput) => execOutput == Outputs[2]; // The ExecOut path allows merging but not the loop. The loop is always a dead end.

	public override List<Connection> GenericConnectionTypeDefined(UndefinedGenericType previousType, Connection connection, TypeBase newType)
	{
		if (Inputs[1].Type.HasUndefinedGenerics)
		{
			var type = newType.Generics[0]; // get the 'T' our of IEnumerable<T>
			Inputs[1].UpdateType(type);

			return [Inputs[1]];
		}

		return new();
	}

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		ArgumentNullException.ThrowIfNull(subChunks);

		var getEnumerator = Inputs[1].Type.MakeRealType().GetMethod(nameof(IEnumerable<int>.GetEnumerator), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
		if (getEnumerator == null)
			throw new Exception($"Unable to find GetEnumerator method on input parameter type: {Inputs[1].Type.FriendlyName}");

		var moveNext = getEnumerator.DeclaringType!.GetMethod(nameof(IEnumerator<int>.MoveNext), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
		if (moveNext == null)
			throw new Exception($"Unable to find MoveNext method on input parameter type: {getEnumerator.DeclaringType!.Name}");

		var current = getEnumerator.DeclaringType.GetProperty(nameof(IEnumerator<int>.Current), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
		if (current == null)
			throw new Exception($"Unable to find Current property on input parameter type: {getEnumerator.DeclaringType!.Name}");

		var enumeratorVariable = Expression.Variable(getEnumerator.DeclaringType!);
		var assignEnumerator = Expression.Assign(enumeratorVariable, Expression.Call(info.LocalVariables[Inputs[1]], getEnumerator));
		var assignCurrent = Expression.Assign(info.LocalVariables[Outputs[1]], Expression.Property(enumeratorVariable, current));

		var loopBody = Expression.Block(
			Graph.BuildExpression(subChunks[Outputs[0]], info)
			.Prepend(assignCurrent)
		);
		var afterLoop = Expression.Block(Graph.BuildExpression(subChunks[Outputs[2]], info));

		var breakLabel = Expression.Label();
		var loop = Expression.Loop(
			Expression.IfThenElse(
				Expression.Call(enumeratorVariable, moveNext), // if the enumerator.MoveNext() returns true
				loopBody, // does the assign for enumerator.Current, as well as the loop body
				Expression.Break(breakLabel) // break the loop
			),
			breakLabel
		);

		// Return a block that does :
		// var enumerator = inputs[1].GetEnumerator();
		// while(enumerator.MoveNext())
		// after loop...
		return Expression.Block([enumeratorVariable], assignEnumerator, loop, afterLoop);
	}
}
