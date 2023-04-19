using NodeDev.Core.Nodes;
using System.Reflection;

namespace NodeDev.Core;

public class Graph
{
	public static Graph Instance { get; } = new();

	public List<Node> Nodes { get; } = new();

	public async Task Invoke(Func<Task> action)
	{
		await action(); // temporary
	}

	public Graph()
	{
		// for test purpose:
		Nodes.Add(new Nodes.Flow.EntryNode(this));
		Nodes.Add(new Nodes.Flow.ReturnNode(this));
	}

}