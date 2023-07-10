using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.NodeDecorations;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes;

public class MethodCall : NormalFlowNode
{
	public class TargetMethodDecoration : INodeDecoration
	{
		private record class SavedMethodInfoParameter(string Type, string SerializedType);
		private record class SavedMethodInfo(string Type, string SerializedType, string Name, SavedMethodInfoParameter[] ParamTypes);
		internal IMethodInfo TargetMethod { get; set; }

		public TargetMethodDecoration(IMethodInfo targetMethod)
		{
			TargetMethod = targetMethod;
		}

		public string Serialize()
		{
			return JsonSerializer.Serialize(new SavedMethodInfo(TargetMethod.DeclaringType.GetType().FullName!, TargetMethod.DeclaringType!.FullName!, TargetMethod.Name, TargetMethod.GetParameters().Select(p => new SavedMethodInfoParameter(p.ParameterType.GetType().FullName!, p.ParameterType.FullName!)).ToArray()));
		}

		public static INodeDecoration Deserialize(TypeFactory typeFactory, string Json)
		{
			var info = JsonSerializer.Deserialize<SavedMethodInfo>(Json) ?? throw new Exception("Unable to deserialize method info");

			var type = TypeBase.Deserialize(typeFactory, info.Type, info.SerializedType);
			var parameterTypes = info.ParamTypes.Select(x => TypeBase.Deserialize(typeFactory, x.Type, x.SerializedType));
			var method = type.GetMethods().FirstOrDefault(x => x.Name == info.Name && parameterTypes.SequenceEqual(x.GetParameters().Select(x => x.ParameterType)));

			if(method == null)
				throw new Exception("Unable to find method:" +  info.Name);

			return new TargetMethodDecoration(method);
		}
	}

	public override string TitleColor => "lightblue";


	internal IMethodInfo? TargetMethod;

	public override IEnumerable<AlternateOverload> AlternatesOverloads
	{
		get
		{
			var parentType = TargetMethod?.DeclaringType;
			if (TargetMethod == null || parentType == null)
				return Enumerable.Empty<AlternateOverload>();

			var methods = parentType.GetMethods().Where(x => x.Name == TargetMethod.Name);

			return methods.Select(x => new AlternateOverload(x.ReturnType, x.GetParameters().ToList())).ToList();
		}
	}

	public MethodCall(Graph graph, string? id = null) : base(graph, id)
	{
	}

	protected override void Deserialize(SerializedNode serializedNodeObj)
	{
		base.Deserialize(serializedNodeObj);

		if (Decorations.TryGetValue(typeof(TargetMethodDecoration), out var targetMethod))
		{
			TargetMethod = ((TargetMethodDecoration)targetMethod).TargetMethod;
			Name = TargetMethod.DeclaringType.FriendlyName + "." + TargetMethod.Name;
		}
	}

	public override void SelectOverload(AlternateOverload overload, out List<Connection> newConnections, out List<Connection> removedConnections)
	{
		// find the MethodInfo for the overload
		var parentType = TargetMethod?.DeclaringType;
		if (TargetMethod == null || parentType == null)
		{
			newConnections = new List<Connection>();
			removedConnections = new List<Connection>();
			return;
		}

		// assumes that every parameters is a real type
		var method = parentType.GetMethods().FirstOrDefault(x => x.Name == TargetMethod.Name && overload.Parameters.Select(x => x.ParameterType).SequenceEqual(overload.Parameters.Select(x => x.ParameterType))); ;
		if (method == null)
			throw new Exception("Unable to find method overload");

		if (method.ReturnType != overload.ReturnType)
			throw new Exception("Return type mismatch");

		// remove the old connections, except the Exec inputs and outputs
		removedConnections = Inputs.Skip(1).Concat(Outputs.Skip(1)).ToList();
		Inputs.RemoveRange(1, Inputs.Count - 1);
		Outputs.RemoveRange(1, Outputs.Count - 1);

		// Set the new method, this will add all the required inputs and outputs
		SetMethodTarget(method);

		// return the new connections
		newConnections = Inputs.Skip(1).Concat(Outputs.Skip(1)).ToList();
	}

	public void SetMethodTarget(IMethodInfo methodInfo)
	{
		TargetMethod = methodInfo;
		Decorations[typeof(TargetMethodDecoration)] = new TargetMethodDecoration(methodInfo);

		Name = TargetMethod.DeclaringType.FriendlyName + "." + TargetMethod.Name;

		if (!TargetMethod.IsStatic)
			Inputs.Insert(0, new("Target", this, TargetMethod.DeclaringType)); // the target is put first for later optimisation as it's not really an input to the method

		// update the inputs
		Inputs.AddRange(TargetMethod.GetParameters().Select(x => new Connection(x.Name!, this, x.ParameterType)));

		if (TargetMethod.ReturnType != TypeFactory.Get(typeof(void)))
			Outputs.Add(new Connection("Result", this, TargetMethod.ReturnType));
	}


	protected override void ExecuteInternal(GraphExecutor executor, object? self, Span<object?> inputs, Span<object?> outputs)
	{
		if (TargetMethod == null)
			throw new Exception("Target method is not set");

		if (TargetMethod is NodeClassMethod nodeClassMethod)
		{
			using var childExecutor = new GraphExecutor(nodeClassMethod.Graph, executor.Root);
			childExecutor.Execute(inputs[0] ?? self, inputs[1..], outputs);
		}
		else
		{
			var realMethod = (NodeClassMethod.RealMethodInfo)TargetMethod;

			var target = TargetMethod.IsStatic ? null : inputs[0];

			var result = realMethod.Method.Invoke(target, inputs[(TargetMethod.IsStatic ? 1 : 2)..].ToArray());

			if (TargetMethod.ReturnType != TypeFactory.Get(typeof(void)))
				outputs[^1] = result;
		}
	}

}
