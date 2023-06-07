﻿using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using System.Collections.Concurrent;
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

	#region Invoke

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

	#endregion

	#region Connect/Disconnect

	public void Connect(Connection connection1, Connection connection2)
	{
		if(!connection1.Connections.Contains(connection2))
			connection1.Connections.Add(connection2);
		if(!connection2.Connections.Contains(connection1))
			connection2.Connections.Add(connection1);
	}

	public void Disconnect(Connection connection1, Connection connection2)
	{
		connection1.Connections.Remove(connection2);
		connection2.Connections.Remove(connection1);
	}

	#endregion

	#region AddNode

	public void AddNode(Node node)
	{
		((IDictionary<string, Node>)Nodes)[node.Id] = node;
	}

	public Node AddNode(NodeProvider.NodeSearchResult searchResult)
	{
		var node = (Node)Activator.CreateInstance(searchResult.Type, this, null)!;
		AddNode(node);
		if (searchResult is NodeProvider.MethodCallNode methodCall && node is Nodes.MethodCall methodCallNode)
			methodCallNode.SetMethodTarget(methodCall.MethodInfo);
		else if (searchResult is NodeProvider.GetPropertyOrFieldNode getPropertyOrField && node is Nodes.GetPropertyOrField getPropertyOrFieldNode)
			getPropertyOrFieldNode.SetMemberTarget(getPropertyOrField.MemberInfo);
		else if (searchResult is NodeProvider.SetPropertyOrFieldNode setPropertyOrField && node is Nodes.GetPropertyOrField setPropertyOrFieldNode)
			setPropertyOrFieldNode.SetMemberTarget(setPropertyOrField.MemberInfo);

		return node;
	}

	#endregion

	#region RemoveNode

	public void RemoveNode(Node node)
	{
		((IDictionary<string, Node>)Nodes).Remove(node.Id);
	}

	#endregion

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