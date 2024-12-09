using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.NodeDecorations;
using NodeDev.Core.Types;
using System.Buffers;
using System.Linq.Expressions;
using System.Text.Json;

namespace NodeDev.Core.Nodes;

public class MethodCall : NormalFlowNode
{
	public class TargetMethodDecoration : INodeDecoration
	{
		private record class SavedMethodInfoParameter(string Type);
		private record class SavedMethodInfo(string Type, string Name, SavedMethodInfoParameter[] ParamTypes);
		internal IMethodInfo TargetMethod { get; set; }

		public TargetMethodDecoration(IMethodInfo targetMethod)
		{
			TargetMethod = targetMethod;
		}

		public string Serialize()
		{
			return JsonSerializer.Serialize(new SavedMethodInfo(TargetMethod.DeclaringType.SerializeWithFullTypeNameString(), TargetMethod.Name, TargetMethod.GetParameters().Select(p => new SavedMethodInfoParameter(p.ParameterType.SerializeWithFullTypeNameString())).ToArray()));
		}

		public static INodeDecoration Deserialize(TypeFactory typeFactory, string Json)
		{
			var info = JsonSerializer.Deserialize<SavedMethodInfo>(Json) ?? throw new Exception("Unable to deserialize method info");

			var type = TypeBase.DeserializeFullTypeNameString(typeFactory, info.Type);
			var parameterTypes = info.ParamTypes.Select(x => TypeBase.DeserializeFullTypeNameString(typeFactory, x.Type));
			var method = type.GetMethods(info.Name).FirstOrDefault(x => parameterTypes.SequenceEqual(x.GetParameters().Select(y => y.ParameterType)));

			if (method == null)
				throw new Exception("Unable to find method:" + info.Name);

			return new TargetMethodDecoration(method);
		}
	}

	public override string TitleColor => "lightblue";


	public IMethodInfo? TargetMethod { get; private set; }

	public override string Name
	{
		get => TargetMethod == null ? "Get" : TargetMethod.DeclaringType.FriendlyName + "." + TargetMethod.Name;
		set { }
	}

	public override IEnumerable<AlternateOverload> AlternatesOverloads
	{
		get
		{
			var parentType = TargetMethod?.DeclaringType;
			if (TargetMethod == null || parentType == null)
				return [];

			var methods = parentType.GetMethods(TargetMethod.Name);

			return methods.Select(x => x.AlternateOverload()).ToList();
		}
	}

	public MethodCall(Graph graph, string? id = null) : base(graph, id)
	{
	}

	protected override void Deserialize(SerializedNode serializedNodeObj)
	{
		base.Deserialize(serializedNodeObj);

		if (Decorations.TryGetValue(typeof(TargetMethodDecoration), out var targetMethod))
			TargetMethod = ((TargetMethodDecoration)targetMethod).TargetMethod;
	}

	public override void SelectOverload(AlternateOverload overload, out List<Connection> newConnections, out List<Connection> removedConnections)
	{
		// find the MethodInfo for the overload
		var parentType = TargetMethod?.DeclaringType;
		if (TargetMethod == null || parentType == null)
		{
			newConnections = [];
			removedConnections = [];
			return;
		}

		var method = parentType
			.GetMethods(TargetMethod.Name)
			.FirstOrDefault(x =>
				x.GetParameters().Select(y => y.ParameterType).SequenceEqual(overload.Parameters.Select(y => y.ParameterType)) && // check if the types match
				x.GetParameters().Select(y => y.IsOut).SequenceEqual(overload.Parameters.Select(y => y.IsOut))); // check if the IsOut match

		if (method == null)
			throw new Exception("Unable to find method overload");

		if (method.ReturnType != overload.ReturnType)
			throw new Exception("Return type mismatch");

		// remove the old connections, except the Exec inputs and outputs
		removedConnections = Inputs.Skip(2).Concat(Outputs.Skip(1)).ToList();
		if (!TargetMethod.IsStatic)
		{
			removedConnections.Add(Inputs[0]); // add the target input
			Inputs.RemoveAt(0);
		}

		if (Inputs.Count != 0)
			Inputs.RemoveRange(1, Inputs.Count - 1);

		Outputs.RemoveRange(1, Outputs.Count - 1);

		// Set the new method, this will add all the required inputs and outputs
		SetMethodTarget(method);

		// return the new connections
		newConnections = Inputs.Take(1).Concat(Inputs.Skip(2)).Concat(Outputs.Skip(1)).ToList();
	}

	public void SetMethodTarget(IMethodInfo methodInfo)
	{
		TargetMethod = methodInfo;
		Decorations[typeof(TargetMethodDecoration)] = new TargetMethodDecoration(methodInfo);

		if (!TargetMethod.IsStatic)
			Inputs.Insert(0, new("Target", this, TargetMethod.DeclaringType)); // the target is put first for later optimisation as it's not really an input to the method

		// update the inputs
		Inputs.AddRange(TargetMethod.GetParameters().Where(x => !x.IsOut).Select(x => new Connection(x.Name, this, x.ParameterType)));

		Outputs.AddRange(TargetMethod.GetParameters().Where(x => x.IsOut).Select(x => new Connection(x.Name, this, x.ParameterType)));

		if (TargetMethod.ReturnType != TypeFactory.Get(typeof(void), null))
			Outputs.Add(new Connection("Result", this, TargetMethod.ReturnType));
	}

	#region Method parameter changes from UI

	internal void OnMethodParameterRenamed(string oldName, NodeClassMethodParameter parameter)
	{
		if (TargetMethod == null)
			throw new Exception("TargetMethod cannot be null here");

		IEnumerable<Connection> connections = parameter.IsOut ? Outputs : Inputs;

		var connection = connections.FirstOrDefault(x => x.Name == oldName);
		if (connection == null)
			throw new Exception("Unable to find connection: " + oldName);

		connection.Name = parameter.Name;

		Graph.RaiseGraphChanged(true);
	}

	internal void OnNewMethodParameter(NodeClassMethodParameter newParameter)
	{
		Inputs.Add(new Connection(newParameter.Name, this, newParameter.ParameterType));

		Graph.RaiseGraphChanged(true);
	}

	#endregion

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		if (subChunks != null)
			throw new Exception("MethodCall.BuildExpression: subChunks should be null as MethodCall never has multiple output paths");
		if (TargetMethod == null)
			throw new Exception("Target method is not set");

		var methodInfo = TargetMethod.CreateMethodInfo();

		// create a method call around the real method with optional target, depending if the method is static or not
		var target = TargetMethod.IsStatic ? null : info.LocalVariables[Inputs[0]];

		var parameters = TargetMethod.GetParameters().Select(x => x.IsOut ? info.LocalVariables[Outputs.First(y => y.Name == x.Name)] : info.LocalVariables[Inputs.First(y => y.Name == x.Name)]).ToArray();

		var call = Expression.Call(target, methodInfo, parameters);

		if (TargetMethod.ReturnType == TypeFactory.Void)
			return call; // no result to assign

		// Assign the call to the output value.
		// This will create an expression that will do both the assignation and the call.
		return Expression.Assign(info.LocalVariables[Outputs[^1]], call);
	}
}
