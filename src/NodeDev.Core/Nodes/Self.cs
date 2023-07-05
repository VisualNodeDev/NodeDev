using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes
{
    public class Self : NoFlowNode
    {
        public Self(Graph graph, string? id = null) : base(graph, id)
        {
            Name = "Self";

            Outputs.Add(new("self", this, TypeFactory.Get(graph.SelfClass)));
        }

		protected override void ExecuteInternal(GraphExecutor executor, object? self, Span<object?> inputs, Span<object?> outputs)
		{
            outputs[0] = self;
        }
    }
}
