using NodeDev.Core.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes
{
    public abstract class NoFlowNode : Node
    {
		public override string TitleColor => "lightgreen";

		public NoFlowNode(Graph graph, string? id = null) : base(graph, id)
        {
        }

        public override bool AlterExecutionStackOnPop => false;

        public override bool IsFlowNode => false;

        public override Connection? Execute(object? self, Connection? inputExec, object?[] inputs, object?[] outputs)
        {
            ExecuteInternal(self, inputs, outputs);

            return null;
        }

        protected abstract void ExecuteInternal(object? self, object?[] inputs, object?[] outputs);
    }
}
