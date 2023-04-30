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
        public override bool AlterExecutionStackOnPop => false;

        public override bool IsFlowNode => true;

        protected NormalFlowNode(Graph graph, string? id = null) : base(graph, id)
        {
            Inputs.Add(new("exec", this, TypeFactory.ExecType));
            Outputs.Add(new("exec", this, TypeFactory.ExecType));
        }

        public override Connection Execute(Connection? inputExec, object?[] inputs, object?[] outputs)
        {
            ExecuteInternal(inputs, outputs);

            return Outputs[0];
        }

        protected abstract void ExecuteInternal(object?[] inputs, object?[] outputs);
    }
}
