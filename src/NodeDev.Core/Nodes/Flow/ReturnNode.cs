using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Flow
{
	public class ReturnNode : Node
	{
		public ReturnNode(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "Return";

			Inputs.Add(new("Exec", this, TypeFactory.ExecType));
		}
	}
}
