using NodeDev.Core.Connections;
using NodeDev.Core.NodeDecorations;
using NodeDev.Core.Types;
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
		public Node(Graph graph, string? id = null)
		{
			Graph = graph;
			Id = id ?? (Guid.NewGuid().ToString());
		}

		public string Id { get; }

		public string Name { get; set; } = "";

		public Graph Graph { get; }

		public List<Connection> Inputs { get; } = new();

		public List<Connection> Outputs { get; } = new();

		public IEnumerable<Connection> InputsAndOutputs => Inputs.Concat(Outputs);

		public abstract bool AlterExecutionStackOnPop { get; }

		public abstract bool IsFlowNode { get; }

		#region Execution

		/// <summary>
		/// Returns the next node to execute. The connection is on the current node, must look at what it's connected to
		/// </summary>
		public abstract Connection? Execute(Connection? connectionBeingExecuted, object?[] inputs, object?[] nodeOutputs);

		protected abstract void ExecuteInternal(object?[] inputs, object?[] outputs);

        #endregion

        #region Decorations

        public Dictionary<Type, INodeDecoration> Decorations { get; init; } = new();

		public void AddDecoration<T>(T attribute) where T : INodeDecoration => Decorations[typeof(T)] = attribute;

		public T GetOrAddDecoration<T>(Func<T> creator) where T : INodeDecoration
		{
			if (Decorations.TryGetValue(typeof(T), out var decoration))
				return (T)decoration;

			var v = creator();
			Decorations[typeof(T)] = v;

			return v;
		}

		#endregion

		#region Serialization

		public record SerializedNode(string Type, string Id, string Name, List<string> Inputs, List<string> Outputs, Dictionary<string, string> Decorations);
		internal string Serialize()
		{
			var serializedNode = new SerializedNode(GetType().FullName!, Id, Name, Inputs.Select(x => x.Serialize()).ToList(), Outputs.Select(x => x.Serialize()).ToList(), Decorations.ToDictionary(x => x.Key.FullName!, x => x.Value.Serialize()));

			return JsonSerializer.Serialize(serializedNode);
		}


        public static Node Deserialize(Graph graph, string serializedNode)
		{
			var serializedNodeObj = JsonSerializer.Deserialize<SerializedNode>(serializedNode) ?? throw new Exception("Unable to deserialize node");

			var type = TypeFactory.GetTypeByFullName(serializedNodeObj.Type) ?? throw new Exception($"Unable to find type: {serializedNodeObj.Type}");
			var node = (Node?)Activator.CreateInstance(type, graph, serializedNodeObj.Id) ?? throw new Exception($"Unable to create instance of type: {serializedNodeObj.Type}");

			foreach(var decoration in serializedNodeObj.Decorations)
			{
				var decorationType = TypeFactory.GetTypeByFullName(decoration.Key) ?? throw new Exception($"Unable to find type: {decoration.Key}");
				
				var method = decorationType.GetMethod(nameof(INodeDecoration.Deserialize), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

				if(method == null)
					throw new Exception($"Unable to find Deserialize method on type: {decoration.Key}");

				var decorationObj = method.Invoke(null, new object[] { decoration.Value }) as INodeDecoration;

				if(decorationObj == null)
					throw new Exception($"Unable to deserialize decoration: {decoration.Key}");

				node.Decorations[decorationType] = decorationObj;
			}

			node.Deserialize(serializedNodeObj);

			return node;
		}

		protected virtual void Deserialize(SerializedNode serializedNodeObj)
		{
			Inputs.Clear();
			Outputs.Clear();

			Name = serializedNodeObj.Name;
			foreach (var input in serializedNodeObj.Inputs)
			{
				var connection = Connection.Deserialize(this, input);
				Inputs.Add(connection);
			}
			foreach (var output in serializedNodeObj.Outputs)
			{
				var connection = Connection.Deserialize(this, output);
				Outputs.Add(connection);
			}
		}

		#endregion
	}
}
