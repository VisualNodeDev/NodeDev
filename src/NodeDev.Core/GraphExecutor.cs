using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core
{
	public class GraphExecutor : IDisposable
	{
		private class State
		{
			public object? Value;
		}
		public readonly Graph Graph;

		private readonly Dictionary<Connection, object?> Connections = new();

		/// <summary>
		/// NodeState is used when some nodes have values they need to remember.
		/// A good example is the ForeachNode, which needs to remember the current state of the enumerator.
		/// </summary>
		private readonly Dictionary<Node, State> NodeStates = new();

		private readonly Stack<GraphExecutor>? ExecutorStack;

		internal readonly GraphExecutor Root;

		public GraphExecutor(Graph graph, GraphExecutor? root)
		{
			Graph = graph;

			if (root == null) // we're the root!
			{
				ExecutorStack = new();
				Root = this;
			}
			else
				Root = root;

			if (Root.ExecutorStack == null)
				throw new Exception("Root should have an executor stack");

			Root.ExecutorStack.Push(this);
		}

		private Node FindEntryNode()
		{
			return Graph.Nodes.First(n => n.Value is EntryNode).Value;
		}

		private readonly State DiscardedState = new State();

		public void Execute(object? self, Span<object?> inputs, Span<object?> outputs)
		{
			var node = FindEntryNode();

			var execConnection = node.Execute(this, self, null, inputs, inputs, ref DiscardedState.Value, out var _);
			if (execConnection == null)
				throw new Exception("Entry node should have an output connection");
			if (inputs.Length != node.Outputs.Count)
				throw new Exception("EntryNode doesn't have the same amount of inputs as the provided inputs array");

			for (int i = 0; i < inputs.Length; ++i)
				Connections[node.Outputs[i]] = inputs[i];


			var stack = new Stack<Connection>(); // stack of input connections on nodes that can alter execution path

			while (true)
			{
				var connectionToExecute = execConnection?.Connections.FirstOrDefault();

				if (connectionToExecute == null) // pop the stack
				{
					if (!stack.TryPop(out var previousInputExec))
						break;

					connectionToExecute = previousInputExec;
				}

				var nodeToExecute = connectionToExecute.Parent;

				if (nodeToExecute is ReturnNode) // we are done
				{
					for (int i = 0; i < outputs.Length; i++)
					{
						var output = nodeToExecute.Inputs[i];
						outputs[i] = CrawlBackInputs(self, output);
					}
					return;
				}

				var nodeInputs = GetNodeInputs(self, nodeToExecute);
				var nodeOutputs = new object?[nodeToExecute.Outputs.Count];

				// Get the state of the node, if necessary
				var state = DiscardedState;
				if (nodeToExecute.FetchState)
				{
					if(!NodeStates.TryGetValue(nodeToExecute, out state))
						NodeStates[nodeToExecute] = state = new State();
				}
				execConnection = nodeToExecute.Execute(this, self, connectionToExecute, nodeInputs, nodeOutputs, ref state.Value, out var alterExecutionStackOnPop);

				for (int i = 0; i < nodeOutputs.Length; i++)
				{
					var output = nodeToExecute.Outputs[i];
					Connections[output] = nodeOutputs[i];
				}

				if (execConnection != null && alterExecutionStackOnPop)
					stack.Push(execConnection);
			}
		}

		private object?[] GetNodeInputs(object? self, Node node)
		{
			var inputs = new object?[node.Inputs.Count];

			for (int i = 0; i < node.Inputs.Count; i++)
			{
				var input = node.Inputs[i];

				inputs[i] = CrawlBackInputs(self, input);
			}

			return inputs;
		}

		private object? CrawlBackInputs(object? self, Connection inputConnection)
		{
			var other = inputConnection.Connections.FirstOrDefault();
			if (other == null)
				return inputConnection.ParsedTextboxValue;

			if (other.Parent.IsFlowNode) // we can stop crawling back and just get the value of the input
				return Connections.TryGetValue(other, out var v) ? v : null;


			// if this is not a flow node, we are allowed to execute the node on demande to calculate the outputs
			// this will also automatically crawl back the inputs of the node we are executing
			var inputs = GetNodeInputs(self, other.Parent);
			var outputs = new object?[other.Parent.Outputs.Count];
			
			other.Parent.Execute(this, self, null, inputs, outputs, ref DiscardedState.Value, out var _);

			object? myOutput = null;
			for (int i = 0; i < outputs.Length; i++)
			{
				var output = other.Parent.Outputs[i];
				Connections[output] = outputs[i];

				if (output == other)
					myOutput = outputs[i];
			}

			return myOutput;
		}

		public void Dispose()
		{
			foreach (var value in Connections)
			{
				if (value.Value is IDisposable disposable)
					disposable.Dispose();
			}
			Connections.Clear();

			if (Root.ExecutorStack == null)
				throw new Exception("Root executor stack shouldn't be null");

			var result = Root.ExecutorStack.Pop();

			if (result != this)
				throw new Exception("GraphExecutor being disposed should always be the last on the stack. Something weird happened!");
		}
	}
}
