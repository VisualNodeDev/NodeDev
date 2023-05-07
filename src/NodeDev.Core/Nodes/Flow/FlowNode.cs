using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Flow
{
	public abstract class FlowNode : Node
	{
		public override string TitleColor => "gray";

		public FlowNode(Graph graph, string? id = null) : base(graph, id)
		{
		}
	}
}
