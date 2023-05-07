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

		internal MethodInfo? TargetMethod;

		public MethodCall(Graph graph, string? id = null) : base(graph, id)
		{
		}

		protected override void Deserialize(SerializedNode serializedNodeObj)
		{
			base.Deserialize(serializedNodeObj);

			if (Decorations.TryGetValue(typeof(TargetMethodDecoration), out var targetMethod))
			{
				TargetMethod = ((TargetMethodDecoration)targetMethod).TargetMethod;
				Name = TargetMethod.DeclaringType!.Name + "." + TargetMethod.Name;
			}
		}

		protected override void ExecuteInternal(object?[] inputs, object?[] outputs)
		{
			if(TargetMethod == null)
				throw new Exception("Target method is not set");

			var target = TargetMethod.IsStatic ? null : inputs[0];
			var result = TargetMethod.Invoke(target, inputs[(TargetMethod.IsStatic ? 1 : 2)..]);

			if (TargetMethod.ReturnType != typeof(void))
				outputs[^1] = result;
		}

		internal void SetMethodTarget(MethodInfo methodInfo)
		{
			TargetMethod = methodInfo;
			Decorations[typeof(TargetMethodDecoration)] = new TargetMethodDecoration(methodInfo);

			Name = TargetMethod.DeclaringType!.Name + "." + TargetMethod.Name;

			if(!TargetMethod.IsStatic)
				Inputs.Add(new("Target", this, TypeFactory.Get(TargetMethod.DeclaringType!)));

			// update the inputs
			Inputs.AddRange(TargetMethod.GetParameters().Select(x => new Connection(x.Name!, this, TypeFactory.Get(x.ParameterType))));

			if (TargetMethod.ReturnType != typeof(void))
				Outputs.Add(new Connection("Result", this, TypeFactory.Get(TargetMethod.ReturnType)));
		}
	}
}
