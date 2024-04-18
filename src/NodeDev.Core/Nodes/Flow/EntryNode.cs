using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Flow
{
    public class EntryNode : FlowNode
    {
        public override string TitleColor => "red";

        public EntryNode(Graph graph, string? id = null) : base(graph, id)
        {
            Name = "Entry";

            Outputs.Add(new("Exec", this, TypeFactory.ExecType));

            Outputs.AddRange(graph.SelfMethod.Parameters.Select(x => new Connection(x.Name, this, x.ParameterType)));
        }

		public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => false;

		public override string GetExecOutputPathId(string pathId, Connection execOutput) => throw new NotImplementedException();

		public override bool IsFlowNode => true;

        public override Connection? Execute(GraphExecutor executor, object? self, Connection? inputExec, Span<object?> inputs, Span<object?> outputs, ref object? state, out bool alterExecutionStackOnPop)
        {
            alterExecutionStackOnPop = false;
            return Outputs[0];
        }

        internal void AddNewParameter(NodeClassMethodParameter newParameter)
        {
            Outputs.Add(new Connection(newParameter.Name, this, newParameter.ParameterType));
        }

        internal void RenameParameter(NodeClassMethodParameter parameter, int index)
        {
            Outputs[index + 1].Name = parameter.Name;
        }

        internal Connection UpdateParameterType(NodeClassMethodParameter parameter, int index)
        {
            var connection = Outputs[index + 1];

            connection.UpdateType(parameter.ParameterType);

            return connection;
        }

        internal void Refresh()
        {
            var removedConnections = Outputs.Skip(1).ToList(); // everything except exec
            var newConnections = Graph.SelfMethod.Parameters.Where(x => !x.IsOut).Select(x => new Connection(x.Name, this, x.ParameterType)).ToList();

            Outputs.RemoveRange(1, Outputs.Count - 1);
            Outputs.AddRange(newConnections);

            Graph.MergedRemovedConnectionsWithNewConnections(newConnections, removedConnections);
        }
    }
}
