using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Math
{
	public class Not: BinaryOperationMath
	{
		public Not(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "Not";
		}

        protected override void ExecuteInternal(GraphExecutor graphExecutor, object? self, Span<object?> inputs, Span<object?> outputs)
		{
			dynamic? a = inputs[0];

			outputs[0] = !a;
        }
    }
}
