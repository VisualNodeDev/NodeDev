using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes;

public class New : NormalFlowNode
{
	public override string Name
	{
		get => Outputs[1].Type.HasUndefinedGenerics ? "New ?" : $"New {Outputs[1].Type.FriendlyName}";
		set { }
	}

	public New(Graph graph, string? id = null) : base(graph, id)
	{
		Outputs.Add(new("Obj", this, new UndefinedGenericType("T")));
	}

	public override IEnumerable<AlternateOverload> AlternatesOverloads
	{
		get
		{
			if (Outputs[1].Type is UndefinedGenericType)
				return [];

			if (Outputs[1].Type is RealType realType)
			{
				var constructors = realType.BackendType.GetConstructors();
				return constructors.Select(x => new AlternateOverload(Outputs[1].Type, x.GetParameters().Select(y => new RealMethodParameterInfo(y, TypeFactory, realType)).OfType<IMethodParameterInfo>().ToList())).ToList();
			}
			else if (Outputs[1].Type is NodeClassType nodeClassType)
				return [new(Outputs[1].Type, [])]; // for now, we don't handle custom constructors

			else
				throw new Exception("Unknowned type in New node: " + Outputs[1].Type.Name);
		}
	}
	public override List<Connection> GenericConnectionTypeDefined(UndefinedGenericType previousType, Connection connection, TypeBase newType)
	{
		var constructor = AlternatesOverloads.First();

		Inputs.AddRange(constructor.Parameters.Select(x => new Connection(x.Name ?? "??", this, x.ParameterType)));

		return [];
	}

	public override void SelectOverload(AlternateOverload overload, out List<Connection> newConnections, out List<Connection> removedConnections)
	{
		removedConnections = Inputs.Skip(1).ToList();
		Inputs.RemoveRange(1, Inputs.Count - 1);

		newConnections = overload.Parameters.Select(x => new Connection(x.Name, this, x.ParameterType)).ToList();
		Inputs.AddRange(newConnections);
	}

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		var type = Outputs[1].Type.MakeRealType();

		var argumentTypes = Inputs.Skip(1).Select(x => x.Type.MakeRealType()).ToArray();
		var constructor = type.GetConstructor(argumentTypes);

		if (constructor == null)
			throw new Exception($"Constructor not found: {Outputs[1].Type.FriendlyName}");

		var arguments = Inputs.Skip(1).Select(x => info.LocalVariables[x]).ToArray();
		return Expression.Assign(info.LocalVariables[Outputs[1]], Expression.New(constructor, arguments));
	}

	protected override void ExecuteInternal(GraphExecutor executor, object? self, Span<object?> inputs, Span<object?> outputs, ref object? state)
	{
		if (Outputs[1].Type is UndefinedGenericType)
			throw new InvalidOperationException("Output type is not defined");

		if (Outputs[1].Type is RealType realType)
			outputs[1] = Activator.CreateInstance(realType.MakeRealType(), inputs[1..].ToArray());
		else if (Outputs[1].Type is NodeClassType nodeClassType)
			outputs[1] = Activator.CreateInstance(Graph.SelfClass.Project.GetCreatedClassType(nodeClassType.NodeClass), inputs[1..].ToArray());
		else
			throw new Exception("Unknown type:" + Outputs[1].Name);
	}
}
