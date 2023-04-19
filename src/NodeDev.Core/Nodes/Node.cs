using NodeDev.Core.Connections;
using NodeDev.Core.NodeDecorations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes
{
	public class Node
	{
		public Node(Graph graph, Guid? id = null)
		{
			Graph = graph;
			Id = id ?? Guid.NewGuid();
		}

		public Guid Id { get; }

		public string Name { get; set; } = "";

		public Graph Graph { get; }

		public List<Connection> Inputs { get; } = new();

		public List<Connection> Outputs { get; } = new();

		#region Decorations

		public Dictionary<Type, NodeDecoration> Decorations { get; init; } = new();

		public void AddDecoration<T>(NodeDecoration attribute) where T : NodeDecoration => Decorations[typeof(T)] = attribute;

		public T GetOrAddAttribute<T>(Func<T> creator) where T: NodeDecoration
		{
			if (Decorations.TryGetValue(typeof(T), out var decoration))
				return (T)decoration;

			var v = creator();
			Decorations[typeof(T)] = v;

			return v;
		}

		#endregion
	}
}
