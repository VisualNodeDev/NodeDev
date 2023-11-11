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


		internal void SwapParameter(int index1, int index2)
		{
			var inputsStart = 1; // skip exec

			var a = Outputs[index1 + inputsStart];
			Outputs[index1 + inputsStart] = Outputs[index2 + inputsStart];
			Outputs[index2 + inputsStart] = a;
		}

		internal void RemoveParameterAt(int index)
		{
			var inputsStart = 1; // skip exec

			var connection = Outputs[index + inputsStart];

			foreach (var other in connection.Connections)
				Graph.Disconnect(other, connection);

			Outputs.RemoveAt(index + inputsStart);
		}
	}
}
