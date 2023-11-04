using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Math
{
	public class SmallerThanOrEqual: NoFlowNode
	{
		public SmallerThanOrEqual(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "SmallerThanOrEqual";

			Inputs.Add(new("a", this, new UndefinedGenericType("T1")));
			Inputs.Add(new("b", this, new UndefinedGenericType("T2")));

			Outputs.Add(new("c", this, TypeFactory.Get<bool>()));
		}

        protected override void ExecuteInternal(GraphExecutor graphExecutor, object? self, Span<object?> inputs, Span<object?> outputs)
		{
			dynamic? a = inputs[0];
			dynamic? b = inputs[1];

			outputs[0] = a <= b;
        }
    }
}
