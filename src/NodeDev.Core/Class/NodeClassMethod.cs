using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Class
{
	public class NodeClassMethod : IMethodInfo
	{
		public record class SerializedNodeClassMethod(string Name, string ReturnType, List<string> Parameters, string Graph);
		public NodeClassMethod(NodeClass ownerClass, string name, TypeBase returnType, Graph graph)
		{
			Class = ownerClass;
			Name = name;
			ReturnType = returnType;
			Graph = graph;

			Graph.SelfMethod = this;
		}

		public NodeClass Class { get; }

		public string Name { get; private set; }

		public TypeBase ReturnType { get; }

		public List<NodeClassMethodParameter> Parameters { get; } = new();

		public Graph Graph { get; }

		public bool IsStatic => false; // not supported yet

		public TypeBase DeclaringType => Class.ClassTypeBase;

		public void Rename(string newName)
		{
			if (string.IsNullOrWhiteSpace(newName))
				return;

			Name = newName;

			Class.Project.GraphChangedSubject.OnNext(Graph);
		}

		public void AddDefaultParameter()
		{
			string name = "NewParameter";
			int i = 2;
			while (Parameters.Any(x => x.Name == name))
                name = $"NewParameter_{i++}";
			var newParameter = new NodeClassMethodParameter(name, Class.TypeFactory.Get<int>(), this);

			Parameters.Add(newParameter);

			foreach (var methodCall in Class.Project.GetNodes<MethodCall>())
			{
				if (methodCall.TargetMethod == this)
				{
					methodCall.OnNewMethodParameter(newParameter);
					Class.Project.GraphChangedSubject.OnNext(methodCall.Graph);
				}
			}

			var entry = Graph.Nodes.Values.OfType<EntryNode>().FirstOrDefault();
			if(entry != null)
			{
				entry.AddNewParameter(newParameter);
				Class.Project.GraphChangedSubject.OnNext(Graph);
			}
		}

		public IEnumerable<IMethodParameterInfo> GetParameters()
		{
			return Parameters;
		}

		public MethodInfo CreateMethodInfo()
		{
			var classType = Class.ClassTypeBase.MakeRealType();

			var method = classType.GetMethod(Name, GetParameters().Select(x => x.ParameterType.MakeRealType()).ToArray());

			if(method == null)
				throw new Exception("Unable to find method: " + Name);

			return method;
		}

		#region Serialization

		private SerializedNodeClassMethod? SavedDataDuringDeserializationStep1 { get; set; }
		public static NodeClassMethod Deserialize(NodeClass owner, string serialized)
		{
			var serializedNodeClassMethod = System.Text.Json.JsonSerializer.Deserialize<SerializedNodeClassMethod>(serialized) ?? throw new Exception("Unable to deserialize node class method");

			var returnType = TypeBase.Deserialize(owner.Project.TypeFactory, serializedNodeClassMethod.ReturnType);
			var graph = new Graph();
			var nodeClassMethod = new NodeClassMethod(owner, serializedNodeClassMethod.Name, returnType, graph);
			graph.SelfMethod = nodeClassMethod; // a bit / really ugly

			foreach (var parameter in serializedNodeClassMethod.Parameters)
				nodeClassMethod.Parameters.Add(NodeClassMethodParameter.Deserialize(owner.Project.TypeFactory, parameter, nodeClassMethod));

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
			var serializedNodeClassMethod = new SerializedNodeClassMethod(Name, ReturnType.SerializeWithFullTypeName(), Parameters.Select(x => x.Serialize()).ToList(), Graph.Serialize());
			return System.Text.Json.JsonSerializer.Serialize(serializedNodeClassMethod);
		}

		#endregion
	}
}
