﻿using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace NodeDev.Core;

public class Graph
{
	public IReadOnlyDictionary<string, Node> Nodes { get; } = new Dictionary<string, Node>();

	public NodeClass SelfClass => SelfMethod.Class;
	public NodeClassMethod SelfMethod { get; set; }

	public Project Project => SelfMethod.Class.Project;

	static Graph()
	{
		NodeProvider.Initialize();
	}

	#region GetChunks

	public class BadMergeException(Connection input) : Exception($"Error merging path to {input.Name} of tool {input.Parent.Name}") { }
	public class DeadEndNotAllowed(List<Connection> inputs) : Exception(inputs.Count == 1 ? $"Dead end not allowed in {inputs[0].Name} of tool {inputs[0].Parent.Name}" : $"Dead end not allowed in {inputs.Count} tools") { }


	/// <summary>
	/// Contains either the straight path betweeh two lines of connections (such as a simple a -> b -> c) or a subchunk of paths.
	/// The subchunk is used to group an entire chunk such as. It would contain "c" and "d" in the following example: 
	///         -> c
	/// a -> b |     -> e
	///         -> d
	/// "e" would be added in the next chunk, along with anything else after it
	/// Only one of the two values is set at once, either <paramref name="Outputs"/> or <paramref name="SubChunk"/>
	/// </summary>
	internal record class NodePathChunkPart(Connection? Output, Dictionary<Connection, NodePathChunks>? SubChunk)
	{
		internal bool ContainOutput(Connection output)
		{
			if (Output == output)
				return true;

			if (SubChunk?.Count > 0)
			{
				foreach (var subChunk in SubChunk)
				{
					if (subChunk.Value.ContainOutput(output))
						return true;
				}
			}

			return false;
		}
	}

	/// <summary>
	/// Contains the starting point of the path, the chunks of the path and the merging point of the path.
	/// If both InputMergePoint and DeadEndInput are null, the path simply led nowhere at all. Should rarely be the case in a valid graph.
	/// </summary>
	/// <param name="OutputStartPoint">Start point of the path.</param>
	/// <param name="Chunks">List of all the chunks inside the path.</param>
	/// <param name="InputMergePoint">Merging point at the end of the path. Null if the path was a dead end or a breaking such as "Return".</param>
	/// <param name="DeadEndInputs">If the path was a dead end, this is the inputs that led to the dead end. Ex if both side of a branch ends in dead end, they will both indicate their last input.</param>
	internal record class NodePathChunks(Connection OutputStartPoint, List<NodePathChunkPart> Chunks, Connection? InputMergePoint, List<Connection>? DeadEndInputs)
	{
		internal bool ContainOutput(Connection output)
		{
			foreach (var part in Chunks)
			{
				if (part.ContainOutput(output))
					return true;
			}

			return false;
		}
	}

	/// <summary>
	/// Get the chunk of a path until the next merging point. In the following example, we would chunk something like "a, (b.c, b.d), e":
	///         -> c
	/// a -> b |     -> e
	///         -> d
	/// if "e" was to be merging with another path, we'd stop at "e" and return it as a merging point.
	/// </summary>
	internal NodePathChunks GetChunks(Connection execOutput, bool allowDeadEnd)
	{
		var chunks = new List<NodePathChunkPart>();

		var currentInput = execOutput.Connections.FirstOrDefault();
		if (currentInput == null)
		{
			if (!allowDeadEnd)
				throw new DeadEndNotAllowed([]);

			return new NodePathChunks(execOutput, chunks, null, null); // the path led nowhere
		}

		while (true)
		{
			if (currentInput.Parent.Outputs.Count(x => x.Type.IsExec) == 1) // we can keep adding to the straight path
			{
				// find the next output node to follow
				var output = currentInput.Parent.Outputs.Single(x => x.Type.IsExec && x != execOutput);
				var nextInput = output.Connections.FirstOrDefault();

				if (nextInput == null)
				{
					// We've reached a dead end, let's check if we were allowed to in the first place
					if (!allowDeadEnd)
						throw new DeadEndNotAllowed([currentInput]);

					return new NodePathChunks(execOutput, chunks, null, [currentInput]); // we reached a dead end
				}

				// add the current node to the chunks, after we know we can keep going
				chunks.Add(new NodePathChunkPart(output, null));

				currentInput = nextInput; // we can keep going

				if (currentInput.Connections.Count != 1)
					return new NodePathChunks(execOutput, chunks, currentInput, null); // we reached a merging point
			}
			else // we have a subchunk
			{
				// Get all the chunks of the node (example, both "c" and "d" in the example above)
				var subChunk = GetChunks(currentInput, currentInput.Parent, allowDeadEnd);

				if (subChunk.Count == 0)
					return new NodePathChunks(execOutput, chunks, null, [currentInput]); // we reached a dead end

				// We had some actual path, add it to the chunks
				var part = new NodePathChunkPart(null, subChunk);
				chunks.Add(part);

				// get the merge point, it should be either null if it's all dead end, or all the same merge point. No need to validate, as GetChunks already did
				var mergePoint = subChunk.Values.FirstOrDefault(x => x.InputMergePoint != null)?.InputMergePoint;

				if (mergePoint != null)
				{
					// we are not the only one merging here, we need to check if all the other paths are sub chunks of ours.
					// That would mean we can keep going, otherwise we need to stop here and let our parent handle the merge.
					foreach (var mergeOutput in mergePoint.Connections)
					{
						if (!part.ContainOutput(mergeOutput))
						{
							// we can't keep going, we need to stop here and let our parent handle the merge
							return new NodePathChunks(execOutput, chunks, mergePoint, null);
						}
					}

					// It's all our stuff, we can keep going

					// we merged back, we can keep going with the next nodes along the path since it's still our path
					currentInput = mergePoint;
				}
				else
				{
					// we reached a dead end, we can stop the path here
					var deadEndInputs = subChunk.Values.Where(x => x.DeadEndInputs != null).ToList();
					if (deadEndInputs.Count != 0) // we have dead ends, not just "nothing connected to the outputs"
						return new NodePathChunks(execOutput, chunks, null, deadEndInputs.SelectMany(x => x.DeadEndInputs!).ToList());
					else
						return new NodePathChunks(execOutput, chunks, null, [currentInput]); // we reached a dead end
				}
			}
		}
	}

	/// <summary>
	/// Get all the chunks of a node. This will recursively get all the chunks of the outputs of the node.
	/// </summary>
	/// <param name="input">Exec input that was used to enter that node.</param>
	/// <param name="node">Node to get all the chunks from.</param>
	/// <param name="allowDeadEnd">Does a parent allow dead end here.</param>
	/// <exception cref="DeadEndNotAllowed"></exception>
	/// <exception cref="BadMergeException"></exception>
	private Dictionary<Connection, NodePathChunks> GetChunks(Connection input, Node node, bool allowDeadEnd)
	{
		var chunks = new Dictionary<Connection, NodePathChunks>();

		foreach (var output in node.Outputs.Where(x => x.Type.IsExec))
		{
			// allowDeadEnd is prioritized over the node's own setting, since we can have a dead end if the parent allows it.
			// Cases like a "branch" inside a loop can be a dead end, even though branch doesn't allow it, because the loop does.
			var chunk = GetChunks(output, allowDeadEnd || node.DoesOutputPathAllowDeadEnd(output));
			chunks[output] = chunk;

			// Validate if the chunk is a merge and if it is allowed
			if (chunk.InputMergePoint != null && !node.DoesOutputPathAllowMerge(output))
				throw new BadMergeException(output);
		}

		if (chunks.Count == 0) // it's a dead end because the node doesn't even have an exec output
			return chunks;

		// Validate that the dead ends are allowed. They are either allowed if the parent path allows it or if they are breaking nodes like "Return"
		if (!allowDeadEnd)
		{
			var hasInvalidDeadEnd = chunks
				.Where(x => !node.DoesOutputPathAllowDeadEnd(x.Key) && (x.Value.DeadEndInputs != null || x.Value.InputMergePoint == null)) // x.Value.InputMergePoint == null is a dead end because the output connection was not connected to anything
				.Where(x => x.Value.DeadEndInputs?.All(y => !y!.Parent.BreaksDeadEnd) ?? true) // ?? true because any dead end by "no connection" is automatically an invalid dead end
				.ToList();

			if (hasInvalidDeadEnd.Count != 0)
			{
				var deadEnds = new List<Connection>();
				foreach (var invalidDeadEnd in hasInvalidDeadEnd)
				{
					if (invalidDeadEnd.Value.DeadEndInputs != null)
						deadEnds.AddRange(invalidDeadEnd.Value.DeadEndInputs);
					else // it's a dead end because the output connection was not connected to anything
						deadEnds.Add(input);
				}

				throw new DeadEndNotAllowed(deadEnds);
			}
		}

		// validate that all the chunks have the same merging point. If not, the path that don't merge at the same place must be dead ends
		var nbDifferentMergePoint = chunks.Values.Where(x => x.InputMergePoint != null).Select(x => x.InputMergePoint).Distinct().Count();
		if (nbDifferentMergePoint > 1) // all the same or none is fine, but more than one is not
			throw new BadMergeException(chunks.Values.First(x => x.InputMergePoint != null).InputMergePoint!); // we can throw any of the inputs, they all have different merging points

		return chunks;
	}

	#endregion

	#region BuildExpression

	public Expression BuildExpression(BuildExpressionOptions options)
	{
		var entry = (Nodes.Values.FirstOrDefault(x => x is EntryNode)?.Outputs.FirstOrDefault()) ?? throw new Exception($"No entry node found in graph {SelfMethod.Name}");
		var returnLabelTarget = SelfMethod.ReturnType == Project.TypeFactory.Void ? Expression.Label("ReturnLabel") : Expression.Label(SelfMethod.ReturnType.MakeRealType(), "ReturnLabel");

		var info = new BuildExpressionInfo(returnLabelTarget, options, SelfMethod.IsStatic ? null : Expression.Parameter(SelfClass.ClassTypeBase.MakeRealType(), "this"));

		// Create a variable for each output parameter
		foreach (var parameter in SelfMethod.Parameters)
		{
			if (parameter.ParameterType.IsExec)
				continue;

			var type = parameter.ParameterType.MakeRealType();
			var variable = Expression.Parameter(type, parameter.Name);
			info.MethodParametersExpression[parameter.Name] = variable;
		}

		// Create a variable for each node input and output
		foreach (var node in Nodes.Values)
		{
			// normal nodes each have their own local variable for every input and output
			foreach ((var connection, var variable) in node.CreateLocalVariableExpressionsForEachInputOutput())
				info.LocalVariables[connection] = variable;
		}

		var chunks = GetChunks(entry, false);

		var expressions = BuildExpression(chunks, info);

		var expressionBlock = Expression.Block(expressions.Append(Expression.Label(returnLabelTarget)));

		return expressionBlock;
	}

	internal static Expression[] BuildExpression(NodePathChunks chunks, BuildExpressionInfo info)
	{
		var expressions = new Expression[chunks.Chunks.Count];

		for (int i = 0; i < chunks.Chunks.Count; ++i)
		{
			var chunk = chunks.Chunks[i];

			if (chunk.Output != null)
			{
				expressions[i] = chunk.Output.Parent.BuildExpression(null, info);
			}
			else if (chunk.SubChunk != null)
			{
				// Each sub chunk has the key of the output connection of that node
				// Therefor, the parent of that output connection is the node itself that we're trying to build
				// such as the "Branch" node with 2 outputs. There will be 2 sub chunks
				var node = chunk.SubChunk.First().Key.Parent;

				expressions[i] = node.BuildExpression(chunk.SubChunk, info);
			}
			else
				throw new Exception("Invalid chunk data, either Output or SubChunk must be set");
		}

		return expressions;
	}

	#endregion

	#region PreprocessGraph

	public int NbConnections { get; private set; }

	internal void PreprocessGraph()
	{
		NbConnections = 0;
		int nodeIndex = 0;
		foreach (var node in Nodes.Values)
		{
			node.GraphIndex = nodeIndex++;
			foreach (var connection in node.InputsAndOutputs)
				connection.GraphIndex = NbConnections++;

			node.PreprocessBeforeExecution();
		}
	}

	#endregion

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

	#region Connect / Disconnect / MergedRemovedConnectionsWithNewConnections

	public void MergedRemovedConnectionsWithNewConnections(List<Connection> newConnections, List<Connection> removedConnections)
	{
		foreach (var removedConnection in removedConnections)
		{
			var newConnection = newConnections.FirstOrDefault(x => x.Parent == removedConnection.Parent && x.Name == removedConnection.Name && x.Type == removedConnection.Type);

			// if we found a new connection, connect them together and remove the old connection
			foreach (var oldLink in removedConnection.Connections)
			{
				oldLink.Connections.Remove(removedConnection); // cleanup the old connection

				if (newConnection != null)
				{
					// Before we re-connect them let's make sure both are inputs or both outputs
					if (oldLink.Parent.Outputs.Contains(oldLink) == newConnection.Parent.Inputs.Contains(newConnection))
					{
						// we can safely reconnect the new connection to the old link
						// Either newConnection is an input and removedConnection is an output or vice versa
						newConnection.Connections.Add(oldLink);
						oldLink.Connections.Add(newConnection);
					}
				}
			}
		}

		Project.GraphChangedSubject.OnNext(this);
	}

	public void Connect(Connection connection1, Connection connection2)
	{
		if (!connection1.Connections.Contains(connection2))
			connection1.Connections.Add(connection2);
		if (!connection2.Connections.Contains(connection1))
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
		if (searchResult is NodeProvider.MethodCallNode methodCall && node is MethodCall methodCallNode)
			methodCallNode.SetMethodTarget(methodCall.MethodInfo);
		else if (searchResult is NodeProvider.GetPropertyOrFieldNode getPropertyOrField && node is GetPropertyOrField getPropertyOrFieldNode)
			getPropertyOrFieldNode.SetMemberTarget(getPropertyOrField.MemberInfo);
		else if (searchResult is NodeProvider.SetPropertyOrFieldNode setPropertyOrField && node is SetPropertyOrField setPropertyOrFieldNode)
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

	public static void Deserialize(string serializedGraph, Graph graph)
	{
		var serializedGraphObj = JsonSerializer.Deserialize<SerializedGraph>(serializedGraph) ?? throw new Exception("Unable to deserialize graph");
		foreach (var serializedNode in serializedGraphObj.Nodes)
		{
			var node = Node.Deserialize(graph, serializedNode);
			graph.AddNode(node);
		}
	}

	#endregion
}