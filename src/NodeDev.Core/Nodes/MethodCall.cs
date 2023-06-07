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

namespace NodeDev.Core.Nodes
{
	public class MethodCall : NormalFlowNode
	{
		public class TargetMethodDecoration : INodeDecoration
		{
			private record class SavedMethodInfo(string Type, string Name, string[] ParamTypes);
			internal MethodInfo TargetMethod { get; set; }

			public TargetMethodDecoration(MethodInfo targetMethod)
			{
				TargetMethod = targetMethod;
			}

			public string Serialize()
			{
				return JsonSerializer.Serialize(new SavedMethodInfo(TargetMethod.DeclaringType!.FullName!, TargetMethod.Name, TargetMethod.GetParameters().Select(p => p.ParameterType.FullName!).ToArray()));
			}

			public static INodeDecoration Deserialize(string Json)
			{
				var info = JsonSerializer.Deserialize<SavedMethodInfo>(Json) ?? throw new Exception("Unable to deserialize method info");
				var type = Type.GetType(info.Type) ?? throw new Exception("Unable to find type: " + info.Type);
				var method = type.GetMethod(info.Name, info.ParamTypes.Select(p => Type.GetType(p)!).ToArray()) ?? throw new Exception("Unable to find method: " + info.Name);

				return new TargetMethodDecoration(method);
			}
		}

		public override string TitleColor => "lightblue";


		internal MethodInfo? TargetMethod;

		public override IEnumerable<AlternateOverload> AlternatesOverloads
		{
			get
			{
				var parentType = TargetMethod?.DeclaringType;
				if (TargetMethod == null || parentType == null)
					return Enumerable.Empty<AlternateOverload>();

				var methods = parentType.GetMethods().Where(x => x.Name == TargetMethod.Name);

				return methods.Select(x => new AlternateOverload(TypeFactory.Get(x.ReturnType), x.GetParameters().Select(y => (y.Name ?? "??", (TypeBase)TypeFactory.Get(y.ParameterType))).ToList()));
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
				Name = TypeFactory.Get(TargetMethod.DeclaringType!).FriendlyName + "." + TargetMethod.Name;
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
			var method = parentType.GetMethod(TargetMethod.Name, overload.Parameters.Select(x => ((RealType)x.Type).BackendType).ToArray());
			if (method == null)
				throw new Exception("Unable to find method overload");

			if (TypeFactory.Get(method.ReturnType) != overload.ReturnType)
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

		internal void SetMethodTarget(MethodInfo methodInfo)
		{
			TargetMethod = methodInfo;
			Decorations[typeof(TargetMethodDecoration)] = new TargetMethodDecoration(methodInfo);

			Name = TypeFactory.Get(TargetMethod.DeclaringType!).FriendlyName + "." + TargetMethod.Name;

			if (!TargetMethod.IsStatic)
				Inputs.Add(new("Target", this, TypeFactory.Get(TargetMethod.DeclaringType!)));

			// update the inputs
			Inputs.AddRange(TargetMethod.GetParameters().Select(x => new Connection(x.Name!, this, TypeFactory.Get(x.ParameterType))));

			if (TargetMethod.ReturnType != typeof(void))
				Outputs.Add(new Connection("Result", this, TypeFactory.Get(TargetMethod.ReturnType)));
		}


		protected override void ExecuteInternal(object? self, object?[] inputs, object?[] outputs)
		{
			if (TargetMethod == null)
				throw new Exception("Target method is not set");

			var target = TargetMethod.IsStatic ? null : inputs[0];
			var result = TargetMethod.Invoke(target, inputs[(TargetMethod.IsStatic ? 1 : 2)..]);

			if (TargetMethod.ReturnType != typeof(void))
				outputs[^1] = result;
		}

	}
}
