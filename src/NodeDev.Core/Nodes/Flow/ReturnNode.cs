using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Flow
{
	public class ReturnNode : FlowNode
	{
		public override string TitleColor => "red";

		public ReturnNode(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "Return";

			Inputs.Add(new("Exec", this, TypeFactory.ExecType));
		}

        public override bool AlterExecutionStackOnPop => false;

        public override bool IsFlowNode => throw new NotImplementedException();

        public override Connection? Execute(object? self, Connection? execInput, object?[] inputs, object?[] outputs)
        {
            return null;
        }

    }
}
