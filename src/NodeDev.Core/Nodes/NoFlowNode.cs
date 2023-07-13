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

        public override bool IsFlowNode => false;

        public override Connection? Execute(GraphExecutor executor, object? self, Connection? inputExec, Span<object?> inputs, Span<object?> outputs, out bool alterExecutionStackOnPop)
		{
            alterExecutionStackOnPop = false;

            ExecuteInternal(executor, self, inputs, outputs);

            return null;
        }

        protected abstract void ExecuteInternal(GraphExecutor graphExecutor, object? self, Span<object?> inputs, Span<object?> outputs);
    }
}
