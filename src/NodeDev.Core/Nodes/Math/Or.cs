using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Math
{
	public class Or: NoFlowNode
	{
		public Or(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "Or";

			Inputs.Add(new("a", this, TypeFactory.Get(typeof(bool))));
			Inputs.Add(new("b", this, TypeFactory.Get(typeof(bool))));

			Outputs.Add(new("c", this, TypeFactory.Get(typeof(bool))));
		}

        protected override void ExecuteInternal(GraphExecutor graphExecutor, object? self, Span<object?> inputs, Span<object?> outputs)
		{
			if (inputs[0] == null || inputs[1] == null)
			{
				outputs[0] = null;
				return;
			}

			var a = (bool)inputs[0]!;
			var b = (bool)inputs[1]!;

			outputs[0] = a || b;
        }
    }
}
