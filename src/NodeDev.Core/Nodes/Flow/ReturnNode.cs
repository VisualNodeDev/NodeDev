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

		public override bool BreaksDeadEnd => true;

		public ReturnNode(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "Return";

			Inputs.Add(new("Exec", this, TypeFactory.ExecType));
		}

        public override bool IsFlowNode => throw new NotImplementedException();

		public override string GetExecOutputPathId(string pathId, Connection execOutput)  => throw new NotImplementedException();

		public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => true; // Return by definition bypasses any dead-end restrictions

		public override Connection? Execute(GraphExecutor executor, object? self, Connection? execInput, Span<object?> inputs, Span<object?> outputs, ref object? state, out bool alterExecutionStackOnPop)
		{
			alterExecutionStackOnPop = false;
			return null;
		}

        internal void Refresh()
        {
			var removedConnections = Inputs.Skip(1).ToList(); // everything except exec
            var newConnections = Graph.SelfMethod.Parameters.Where(x => x.IsOut).Select(x => new Connection(x.Name, this, x.ParameterType)).ToList();

			if (Graph.SelfMethod.ReturnType != TypeFactory.Void)
				newConnections.Add(new Connection("Return", this, Graph.SelfMethod.ReturnType));

			Inputs.RemoveRange(1, Inputs.Count - 1);
            Inputs.AddRange(newConnections);

            Graph.MergedRemovedConnectionsWithNewConnections(newConnections, removedConnections);
        }
    }
}
