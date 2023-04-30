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
        public NoFlowNode(Graph graph, string? id = null) : base(graph, id)
        {
        }

        public override bool AlterExecutionStackOnPop => false;

        public override bool IsFlowNode => false;

        public override Connection? Execute(Connection? inputExec, object?[] inputs, object?[] outputs)
        {
            ExecuteInternal(inputs, outputs);

            return null;
        }
    }
}
