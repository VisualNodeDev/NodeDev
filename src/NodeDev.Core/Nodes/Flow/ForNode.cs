﻿using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Flow
{
	public class ForNode : FlowNode
	{
		public override bool IsFlowNode => true;

		public override bool FetchState => true;

		public override bool ReOrderExecInputsAndOutputs => false;

		public ForNode(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "For";

			Inputs.Add(new("Exec", this, TypeFactory.ExecType));
			Inputs.Add(new("Start", this, TypeFactory.Get<int>()));
			Inputs.Add(new("End (Exclude)", this, TypeFactory.Get<int>()));

			Outputs.Add(new("ExecLoop", this, TypeFactory.ExecType));
			Outputs.Add(new("Index", this, TypeFactory.Get<int>()));
			Outputs.Add(new("ExecOut", this, TypeFactory.ExecType));
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
}
