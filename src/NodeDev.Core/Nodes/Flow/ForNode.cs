using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Flow;

public class ForNode : FlowNode
{
	public override bool IsFlowNode => true;

	public override bool FetchState => true;

	public override bool ReOrderExecInputsAndOutputs => false;

	public override bool AllowRemergingExecConnections => false;

	public ForNode(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "For";

		Inputs.Add(new("Exec", this, TypeFactory.ExecType));
		Inputs.Add(new("Start", this, TypeFactory.Get<int>()));
		Inputs.Add(new("End (Exclude)", this, TypeFactory.Get<int>()));

		Outputs.Add(new("ExecLoop", this, TypeFactory.ExecType));
		Outputs.Add(new("Index", this, TypeFactory.Get<int>(), linkedExec: Outputs[0]));
		Outputs.Add(new("ExecOut", this, TypeFactory.ExecType));
	}

	public override string GetExecOutputPathId(string pathId, Connection execOutput)
	{
		if (execOutput == Outputs[0])
			return pathId + "-" + execOutput.Id;
		else if (execOutput == Outputs[2])
			return pathId;
		else
			throw new Exception("Invalid exec output");
	}

	public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => execOutput == Outputs[0]; // The loop exec path must be a dead end (or a breaking node, such as return, continue, break)

        public override bool DoesOutputPathAllowMerge(Connection execOutput) => execOutput == Outputs[2]; // the ExecOut path allows merging, but not the loop. The loop is always a dead end.

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		throw new NotImplementedException();
	}

	public override Connection? Execute(GraphExecutor executor, object? self, Connection? connectionBeingExecuted, Span<object?> inputs, Span<object?> nodeOutputs, ref object? state, out bool alterExecutionStackOnPop)
	{
		// check if we're looping of we're starting a new loop
		int i;
		if (connectionBeingExecuted == Inputs[0]) // start the loop
		{
			i = (int)inputs[1]!; // start at the beginning of the loop
			state = i; // start at the beginning of the loop
		}
		else // continue the loop
		{
			i = (int)state! + 1; // move to the next item in the loop
			state = i;
		}

		// check if we're done
		if (i >= (int)inputs[2]!) // if we're done
		{
			alterExecutionStackOnPop = false;
			return Outputs[2]; // execute the ExecOut
		}

		nodeOutputs[1] = i; // output the current index
		alterExecutionStackOnPop = true;
		return Outputs[0]; // execute the ExecLoop
	}
}
