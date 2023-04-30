using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Math
{
	public abstract class BinaryOperationMath: NoFlowNode
	{

		public BinaryOperationMath(Graph graph, string? id = null) : base(graph, id)
		{
			var t1 = TypeFactory.CreateGenericType("T1");
			var t2 = TypeFactory.CreateGenericType("T2");
			Inputs.Add(new("a", this, t1));
			Outputs.Add(new("b", this, t2));
		}
	}
}
