using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Math
{
	public class BiggerThan: NoFlowNode
	{
		public BiggerThan(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "BiggerThan";

			var t1 = TypeFactory.CreateUndefinedGenericType("T1");
			var t2 = TypeFactory.CreateUndefinedGenericType("T2");
			Inputs.Add(new("a", this, t1));
			Inputs.Add(new("b", this, t2));

			Outputs.Add(new("c", this, TypeFactory.Get<bool>()));
		}

        protected override void ExecuteInternal(object?[] inputs, object?[] outputs)
        {
			dynamic? a = inputs[0];
			dynamic? b = inputs[1];

			outputs[0] = a > b;
        }
    }
}
