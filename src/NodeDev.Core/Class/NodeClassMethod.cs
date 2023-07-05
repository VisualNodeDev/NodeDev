using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Class
{
    public class NodeClassMethod: IMethodInfo
    {

		public class RealMethodInfo : IMethodInfo
		{
			private readonly TypeFactory TypeFactory;

			public readonly MethodInfo Method;

			public RealMethodInfo(TypeFactory typeFactory, MethodInfo method)
			{
				TypeFactory = typeFactory;
				Method = method;
			}

			public string Name => Method.Name;

			public bool IsStatic => Method.IsStatic;

			public TypeBase DeclaringType => TypeFactory.Get(Method.DeclaringType!);

			public TypeBase ReturnType => TypeFactory.Get(Method.ReturnType);

			public IEnumerable<IMethodParameterInfo> GetParameters()
			{
				return Method.GetParameters().Select(x => new RealMethodParameterInfo(x, TypeFactory));
			}

			public class RealMethodParameterInfo : IMethodParameterInfo
			{
				public readonly ParameterInfo ParameterInfo;

				public readonly TypeFactory TypeFactory;

				public RealMethodParameterInfo(ParameterInfo parameterInfo, TypeFactory typeFactory)
				{
					ParameterInfo = parameterInfo;
					TypeFactory = typeFactory;
				}

				public string Name => ParameterInfo.Name ?? "";

				public TypeBase ParameterType => TypeFactory.Get(ParameterInfo.ParameterType);
			}
		}


		public record class SerializedNodeClassMethod(string Name, string ReturnTypeFullName, string ReturnType, List<string> Parameters, string Graph);
		public NodeClassMethod(NodeClass ownerClass, string name, TypeBase returnType, Graph graph)
		{
			Class = ownerClass;
			Name = name;
			ReturnType = returnType;
			Graph = graph;
		}


        public NodeClass Class { get; }

        public string Name { get; private set; }

        public TypeBase ReturnType { get; }

        public List<NodeClassMethodParameter> Parameters { get; } = new();

        public Graph Graph { get; }

		public bool IsStatic => false;

		public TypeBase DeclaringType => Class.TypeFactory.Get(Class);


		public void Rename(string newName)
		{
			if(string.IsNullOrWhiteSpace(newName)) 
				return;

			Name = newName;
		}

		public IEnumerable<IMethodParameterInfo> GetParameters()
		{
			return Parameters;
		}

		#region Serialization

		private SerializedNodeClassMethod? SavedDataDuringDeserializationStep1 { get; set; }
		public static NodeClassMethod Deserialize(NodeClass owner, string serialized)
        {
            var serializedNodeClassMethod = System.Text.Json.JsonSerializer.Deserialize<SerializedNodeClassMethod>(serialized) ?? throw new Exception("Unable to deserialize node class method");

            var returnType = TypeBase.Deserialize(owner.Project.TypeFactory, serializedNodeClassMethod.ReturnTypeFullName, serializedNodeClassMethod.ReturnType);
			var graph = new Graph();
			var nodeClassMethod = new NodeClassMethod(owner, serializedNodeClassMethod.Name, returnType, graph);
			graph.SelfMethod = nodeClassMethod; // a bit / really ugly

			foreach (var parameter in serializedNodeClassMethod.Parameters)
				nodeClassMethod.Parameters.Add(NodeClassMethodParameter.Deserialize(owner.Project.TypeFactory, parameter));

			nodeClassMethod.SavedDataDuringDeserializationStep1 = serializedNodeClassMethod;

			return nodeClassMethod;
        }

		public void Deserialize_Step3()
		{
			if (SavedDataDuringDeserializationStep1 == null)
				throw new Exception("Cannot call Deserialize_Step3 before calling Deserialize");

			Graph.Deserialize(SavedDataDuringDeserializationStep1.Graph, Graph);

			SavedDataDuringDeserializationStep1 = null;
		}

		public string Serialize()
        {
			var serializedNodeClassMethod = new SerializedNodeClassMethod(Name, ReturnType.GetType().FullName!, ReturnType.FullName, Parameters.Select(x => x.Serialize()).ToList(), Graph.Serialize());
			return System.Text.Json.JsonSerializer.Serialize(serializedNodeClassMethod);
		}

		#endregion
	}
}
