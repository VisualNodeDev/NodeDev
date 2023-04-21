using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Math
{
	public class Add: TwoOperationMath
	{
		public Add(Graph graph, Guid? id = null) : base(graph, id)
		{
		}
	}
}
