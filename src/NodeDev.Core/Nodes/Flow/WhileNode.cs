using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Flow
{
	public class WhileNode : FlowNode
	{

		public override bool IsFlowNode => true;

		public override bool AllowRemergingExecConnections => false;

		public WhileNode(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "While";

			Inputs.Add(new("Exec", this, TypeFactory.ExecType));
			Inputs.Add(new("Condition", this, TypeFactory.Get<bool>()));

			Outputs.Add(new("ExecLoop", this, TypeFactory.ExecType));
			Outputs.Add(new("ExecOut", this, TypeFactory.ExecType));
		}

		public override Connection? Execute(GraphExecutor executor, object? self, Connection? connectionBeingExecuted, Span<object?> inputs, Span<object?> nodeOutputs, ref object? state, out bool alterExecutionStackOnPop)
		{
			if (inputs[1] is bool b && b == true)
			{
				alterExecutionStackOnPop = true; // re-execute the 'while' when this line is done
				return Outputs[0];
			}
			else
			{
				alterExecutionStackOnPop = false;
				return Outputs[1];
			}
		}
	}
}
