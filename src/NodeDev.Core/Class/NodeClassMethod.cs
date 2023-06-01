using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Class
{
    public class NodeClassMethod
    {
		private record class SerializedNodeClassMethod(string Name, string ReturnTypeFullName, string ReturnType, List<string> Parameters, string Graph);
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

		public void Rename(string newName)
		{
			if(string.IsNullOrWhiteSpace(newName)) 
				return;

			Name = newName;
		}

		#region Serialization

		public static NodeClassMethod Deserialize(NodeClass owner, string serialized)
        {
            var serializedNodeClassMethod = System.Text.Json.JsonSerializer.Deserialize<SerializedNodeClassMethod>(serialized) ?? throw new Exception("Unable to deserialize node class method");

            var returnType = TypeBase.Deserialize(serializedNodeClassMethod.ReturnTypeFullName, serializedNodeClassMethod.ReturnType);
            var graph = Graph.Deserialize(serializedNodeClassMethod.Graph);
			var nodeClassMethod = new NodeClassMethod(owner, serializedNodeClassMethod.Name, returnType, graph);

            foreach (var parameter in serializedNodeClassMethod.Parameters)
				nodeClassMethod.Parameters.Add(NodeClassMethodParameter.Deserialize(parameter));

            return nodeClassMethod;
        }

        public string Serialize()
        {
			var serializedNodeClassMethod = new SerializedNodeClassMethod(Name, ReturnType.GetType().FullName!, ReturnType.FullName, Parameters.Select(x => x.Serialize()).ToList(), Graph.Serialize());
			return System.Text.Json.JsonSerializer.Serialize(serializedNodeClassMethod);
		}

		#endregion
	}
}
