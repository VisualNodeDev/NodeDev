using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Math
{
	public class Modulo: TwoOperationMath
	{
		public Modulo(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "Modulo";
		}

        protected override void ExecuteInternal(object?[] inputs, object?[] outputs)
        {
			dynamic? a = inputs[0];
			dynamic? b = inputs[1];

			outputs[0] = a % b;
        }
    }
}
