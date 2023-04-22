using NodeDev.Core.Connections;
using NodeDev.Core.NodeDecorations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes
{
	public abstract class Node
	{
		public Node(Graph graph, Guid? id = null)
		{
			Graph = graph;
			Id = (id ?? Guid.NewGuid()).ToString();
		}

		public string Id { get; }

		public string Name { get; set; } = "";

		public Graph Graph { get; }

		public List<Connection> Inputs { get; } = new();

		public List<Connection> Outputs { get; } = new();

		public IEnumerable<Connection> InputsAndOutputs => Inputs.Concat(Outputs);

		#region Decorations

		public Dictionary<Type, NodeDecoration> Decorations { get; init; } = new();

		public void AddDecoration<T>(T attribute) where T : NodeDecoration => Decorations[typeof(T)] = attribute;

		public T GetOrAddDecoration<T>(Func<T> creator) where T: NodeDecoration
		{
			if (Decorations.TryGetValue(typeof(T), out var decoration))
				return (T)decoration;

			var v = creator();
			Decorations[typeof(T)] = v;

			return v;
		}

		#endregion

		#region Serialization

        public record SerializedNode(string Type, string Id, string Name, List<string> Inputs, List<string> Outputs);
		internal string Serialize()
		{
			var serializedNode = new SerializedNode(GetType().FullName!, Id, Name, Inputs.Select(x => x.Serialize()).ToList(), Outputs.Select(x => x.Serialize()).ToList());

			return JsonSerializer.Serialize(serializedNode);
		}

		public static Node Deserialize(Graph graph, string serializedNode)
		{
			var serializedNodeObj = JsonSerializer.Deserialize<SerializedNode>(serializedNode) ?? throw new Exception("Unable to deserialize node");

			var type = Type.GetType(serializedNodeObj.Type) ?? throw new Exception($"Unable to find type: {serializedNodeObj.Type}");
			var node = (Node?)Activator.CreateInstance(type, graph, serializedNodeObj.Id) ?? throw new Exception($"Unable to create instance of type: {serializedNodeObj.Type}");

			node.Deserialize(serializedNodeObj);

			return node;
		}

		protected virtual Dictionary<Connection, List<Connection.SerializedConnection>> Deserialize(SerializedNode serializedNodeObj)
		{
			var connections = new Dictionary<Connection, List<Connection.SerializedConnection>>();

			Name = serializedNodeObj.Name;
			foreach (var input in serializedNodeObj.Inputs)
			{
				var connection = Connection.Deserialize(this, input, out var serializedConnectionObj);
				Inputs.Add(connection);

				if(!connections.TryGetValue(connection, out var list))
					connections[connection] = list = new List<Connection.SerializedConnection>();
				
				list.Add(serializedConnectionObj);
			}
			foreach (var output in serializedNodeObj.Outputs)
			{
				var connection = Connection.Deserialize(this, output, out var serializedConnectionObj);
				Outputs.Add(connection);

				if (!connections.TryGetValue(connection, out var list))
					connections[connection] = list = new List<Connection.SerializedConnection>();

				list.Add(serializedConnectionObj);
			}

			return connections;
		}

		#endregion
	}
}
