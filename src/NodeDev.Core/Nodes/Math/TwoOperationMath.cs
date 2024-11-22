using NodeDev.Core.Connections;
using NodeDev.Core.Types;

namespace NodeDev.Core.Nodes.Math
{
	public abstract class TwoOperationMath : NoFlowNode
	{
		protected abstract string OperatorName { get; }

		public TwoOperationMath(Graph graph, string? id = null) : base(graph, id)
		{
			Inputs.Add(new("a", this, new UndefinedGenericType("T1")));
			Inputs.Add(new("b", this, new UndefinedGenericType("T2")));

			Outputs.Add(new("c", this, new UndefinedGenericType("T3")));
		}

		public override List<Connection> GenericConnectionTypeDefined(Connection connection)
		{
			if (Inputs.Count(x => x.Type is RealType t && (t.BackendType.IsPrimitive || t.BackendType == typeof(string))) == 2)
			{
				if (!Outputs[0].Type.HasUndefinedGenerics)
					return new();

				var type1 = (Inputs[0].Type as RealType)!.BackendType;
				var type2 = (Inputs[1].Type as RealType)!.BackendType;

				Type resultingType;
				// both inputs are basic types like int or float
				// find the type with the highest precision
				if (type1 == typeof(string) || type2 == typeof(string))
					resultingType = typeof(string);
				else if (type1 == typeof(decimal) || type2 == typeof(decimal))
					resultingType = typeof(decimal);
				else if (type1 == typeof(double) || type2 == typeof(double))
					resultingType = typeof(double);
				else if (type1 == typeof(float) || type2 == typeof(float))
					resultingType = typeof(float);
				else if (type1 == typeof(long) || type2 == typeof(long))
					resultingType = typeof(long);
				else if (type1 == typeof(uint) && type2 == typeof(uint))
					resultingType = typeof(uint);
				else if ((type1 == typeof(uint) && type2 == typeof(int)) || (type2 == typeof(uint) && type1 == typeof(int)))
					resultingType = typeof(long);
				else
					resultingType = typeof(int);

				Outputs[0].UpdateTypeAndTextboxVisibility(TypeFactory.Get(resultingType, null), overrideInitialType: true);

				return new() { Outputs[0] };
			}
			else if (Inputs[0].Type is RealType type1 && Inputs[1].Type is RealType type2)
			{
				var operationName = "op_" + OperatorName;
				var operations = type1.BackendType.GetMethods().Where(x => x.IsSpecialName && x.Name == operationName);

				var correctOne = operations.FirstOrDefault(x => x.GetParameters().Length == 2 && x.GetParameters()[1].ParameterType == type2.BackendType);

				if (correctOne != null)
				{
					Outputs[0].UpdateTypeAndTextboxVisibility(TypeFactory.Get(correctOne.ReturnType, null), overrideInitialType: true);
					return new() { Outputs[0] };
				}
			}

			return new();
		}

	}
}
