using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Debug
{
    public class WriteLine : NormalFlowNode
    {
        public WriteLine(Graph graph, string? id = null) : base(graph, id)
        {
            Name = "WriteLine";

            Inputs.Add(new("Line", this, TypeFactory.CreateUndefinedGenericType("T")));
        }

        protected override void ExecuteInternal(GraphExecutor executor, object? self, Span<object?> inputs, Span<object?> outputs)
		{
            Console.WriteLine(inputs[1]);
        }
    }
}
