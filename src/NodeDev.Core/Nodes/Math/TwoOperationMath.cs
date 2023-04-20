using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Math
{
	public class TwoOperationMath<T1, T2, T3> : Node
	{
		public TwoOperationMath(Graph graph, Guid? id = null) : base(graph, id)
		{
			var t1 = TypeFactory.CreateGenericType(nameof(T1));
			var t2 = TypeFactory.CreateGenericType(nameof(T2));
			var t3 = TypeFactory.CreateGenericType(nameof(T3));
			Inputs.Add(new("a", this, t1));
			Inputs.Add(new("b", this, t2));

			Outputs.Add(new("c", this, t3));
		}
	}
}
