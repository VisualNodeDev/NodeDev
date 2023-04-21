using NodeDev.Core.Nodes;
using System.Reflection;

namespace NodeDev.Core;

public class Graph
{
	public static Graph Instance { get; } = new();

	static Graph()
	{
		NodeProvider.Initialize();
	}

	public IReadOnlyDictionary<string, Node> Nodes { get; } = new Dictionary<string, Node>();

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

	public Graph()
	{
		// for test purpose:
		AddNode(new Nodes.Flow.EntryNode(this));
		AddNode(new Nodes.Flow.ReturnNode(this));
	}

}