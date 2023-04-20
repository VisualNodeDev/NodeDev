using NodeDev.Core.Nodes;
using System.Reflection;

namespace NodeDev.Core;

public class Graph
{
	public static Graph Instance { get; } = new();

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

	public Graph()
	{
		// for test purpose:
		AddNode(new Nodes.Flow.EntryNode(this));
		AddNode(new Nodes.Flow.ReturnNode(this));
	}

}