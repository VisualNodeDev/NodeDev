using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Flow
{
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

		public override Connection? Execute(GraphExecutor executor, object? self, Connection? connectionBeingExecuted, Span<object?> inputs, Span<object?> nodeOutputs, ref object? state, out bool alterExecutionStackOnPop)
		{
			// check if we're looping of we're starting a new loop
			IEnumerator<object?> enumeratorState;
			if (connectionBeingExecuted == Inputs[0]) // start the loop
				state = enumeratorState = ((IEnumerable<object?>)inputs[1]!).GetEnumerator();
			else // continue the loop
				enumeratorState = (IEnumerator<object?>)state!;

			// get the next item
			if (!enumeratorState.MoveNext())
			{
				alterExecutionStackOnPop = false;
				nodeOutputs[1] = null;
				return Outputs[2];
			}

			nodeOutputs[1] = enumeratorState.Current;
			alterExecutionStackOnPop = true;
			return Outputs[0];
		}
	}
}
