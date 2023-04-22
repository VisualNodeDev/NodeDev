using NodeDev.Core.Nodes;
using System.Reflection;
using System.Text.Json;

namespace NodeDev.Core;

public class Graph
{
	public IReadOnlyDictionary<string, Node> Nodes { get; } = new Dictionary<string, Node>();


	static Graph()
	{
		NodeProvider.Initialize();
	}

	public Task Invoke(Action action)
	{
		return Invoke(() =>
		{
			action();
			return Task.CompletedTask;
		});
	}

	public async Task Invoke(Func<Task> action)
	{
		await action(); // temporary
	}

	public void AddNode(Node node)
	{
		((IDictionary<string, Node>)Nodes)[node.Id] = node;
	}

	public Node AddNode(Type type)
	{
		var node = (Node)Activator.CreateInstance(type, this, null)!;
		AddNode(node);
		return node;
	}

	public void RemoveNode(Node node)
	{
		((IDictionary<string, Node>)Nodes).Remove(node.Id);
	}


	#region Serialization

	private record class SerializedGraph(List<string> Nodes);
	public string Serialize()
	{
		var nodes = new List<string>();

		foreach (var node in Nodes.Values)
			nodes.Add(node.Serialize());

		var serializedGraph = new SerializedGraph(nodes);

		return JsonSerializer.Serialize(serializedGraph);
	}

	public static Graph Deserialize(string serializedGraph)
	{
		var graph = new Graph();
		var serializedGraphObj = JsonSerializer.Deserialize<SerializedGraph>(serializedGraph) ?? throw new Exception("Unable to deserialize graph");
		foreach (var serializedNode in serializedGraphObj.Nodes)
		{
			var node = Node.Deserialize(graph, serializedNode);
			graph.AddNode(node);
		}
		return graph;
	}

	#endregion
}