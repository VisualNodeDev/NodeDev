using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Flow
{
	public class EntryNode : Node
	{
		public EntryNode(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "Entry";

			Outputs.Add(new("Exec", this, TypeFactory.ExecType));
		}

        public override bool AlterExecutionStackOnPop => false;

        public override bool IsFlowNode => true;

        public override Connection? Execute(Connection? inputExec, object?[] inputs, object?[] outputs)
        {
            ExecuteInternal(inputs, outputs);

            return Outputs[0];
        }

        protected override void ExecuteInternal(object?[] inputs, object?[] outputs)
        {
        }
    }
}
