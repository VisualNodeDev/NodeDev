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
			Inputs.Add(new("a", this, new UndefinedGenericType("T1")));
			Outputs.Add(new("b", this, new UndefinedGenericType("T2")));
		}
	}
}
