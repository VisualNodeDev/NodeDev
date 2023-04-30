using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Flow
{
	public class Branch : Node
	{

		public override bool AlterExecutionStackOnPop => false;

		public override bool IsFlowNode => true;

		public Branch(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "Branch";

			Inputs.Add(new("Exec", this, TypeFactory.ExecType));
			Inputs.Add(new("Condition", this, TypeFactory.Get<bool>()));

			Outputs.Add(new("IfTrue", this, TypeFactory.ExecType));
			Outputs.Add(new("IfFalse", this, TypeFactory.ExecType));
		}

		public override Connection? Execute(Connection? connectionBeingExecuted, object?[] inputs, object?[] nodeOutputs)
		{
			if (inputs[1] is bool b && b == true)
				return Outputs[0];
			else
				return Outputs[1];
		}
	}
}
