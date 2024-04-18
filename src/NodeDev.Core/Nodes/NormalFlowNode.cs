using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes
{
    /// <summary>
    /// Nodes that want to implement a normal flow should inherit from this class.
    /// It'll automatically add a input exec and output exec.
    /// </summary>
    public abstract class NormalFlowNode : Node
    {
		public override string TitleColor => "lightblue";

        public override bool IsFlowNode => true;

		public override string GetExecOutputPathId(string pathId, Connection execOutput)
		{
            if(execOutput != Outputs[0])
				throw new InvalidOperationException("Invalid exec output connection.");

            return pathId; // no need to change the pathId, we're just flowing through like a->b->c
		}

        public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => false; // A normal flow node should not allow dead ends, unless a parent allowed it.

		protected NormalFlowNode(Graph graph, string? id = null) : base(graph, id)
        {
            Inputs.Add(new("Exec", this, TypeFactory.ExecType));
            Outputs.Add(new("Exec", this, TypeFactory.ExecType));
        }

        public override Connection Execute(GraphExecutor executor, object? self, Connection? inputExec, Span<object?> inputs, Span<object?> outputs, ref object? state, out bool alterExecutionStackOnPop)
		{
            alterExecutionStackOnPop = false;

			ExecuteInternal(executor, self, inputs, outputs, ref state);

            return Outputs[0];
        }

        protected abstract void ExecuteInternal(GraphExecutor executor, object? self, Span<object?> inputs, Span<object?> outputs, ref object? state);
    }
}
