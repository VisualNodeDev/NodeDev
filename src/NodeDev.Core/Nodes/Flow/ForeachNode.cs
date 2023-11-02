using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Flow
{
	public class ForeachNode : FlowNode
	{
		public override bool IsFlowNode => true;

		public ForeachNode(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "Foreach";

			var t = TypeFactory.CreateUndefinedGenericType("T");

			Inputs.Add(new("Exec", this, TypeFactory.ExecType));
			Inputs.Add(new("IEnumerable", this, TypeFactory.Get(typeof(IEnumerable<>), new[] {t})));

			Outputs.Add(new("Item", this, t));
			Outputs.Add(new("ExecLoop", this, TypeFactory.ExecType));
			Outputs.Add(new("ExecOut", this, TypeFactory.ExecType));
		}

		public override List<Connection> GenericConnectionTypeDefined(UndefinedGenericType previousType, Connection connection, TypeBase newType)
		{
			if (Inputs[1].Type.HasUndefinedGenerics)
			{
				var type = newType.Generics[0]; // get the 'T' our of IEnumerable<T>
				Inputs[1].UpdateType(type);

				return new() { Inputs[1] };
			}

			return new();
		}


		public override Connection? Execute(GraphExecutor executor, object? self, Connection? connectionBeingExecuted, Span<object?> inputs, Span<object?> nodeOutputs, out bool alterExecutionStackOnPop)
		{
			if (inputs[1] is bool b && b == true)
			{
				alterExecutionStackOnPop = true; // re-execute the 'while' when this line is done
				return Outputs[0];
			}
			else
			{
				alterExecutionStackOnPop = false;
				return Outputs[1];
			}
		}
	}
}
