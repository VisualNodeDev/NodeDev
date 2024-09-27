using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.NodeDecorations;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NodeDev.Core.Nodes
{
	[System.Diagnostics.DebuggerDisplay("{Name}. Inputs: {Inputs.Count}. Outputs: {Outputs.Count}")]
	public abstract class Node
	{
		public Node(Graph graph, string? id = null)
		{
			Graph = graph;
			Id = id ?? (Guid.NewGuid().ToString());
		}

		public string Id { get; }

		public virtual string Name { get; set; } = "";

		public Graph Graph { get; }

		public abstract string TitleColor { get; }

		public List<Connection> Inputs { get; } = new();

		public List<Connection> Outputs { get; } = new();

		public IEnumerable<Connection> InputsAndOutputs => Inputs.Concat(Outputs);

		public abstract bool IsFlowNode { get; }

		public TypeFactory TypeFactory => Project.TypeFactory;

		public Project Project => Graph.SelfClass.Project;

		public virtual bool FetchState => false;

		public virtual bool ReOrderExecInputsAndOutputs => true;

		public bool CanBeInlined => !InputsAndOutputs.Any(x => x.Type.IsExec);

		/// <summary>
		/// True to allow remerging exec connection together later in the graph.
		/// This is used by nodes that have multiple exec outputs such as Branch, Loop, etc.
		/// If the value is false, such as for Loop, each exec output has to be a separate path.
		/// Ex, for ForEach node the value is false since "Loop Exec" path cannot have shared path with the "ExecOut" path.
		/// </summary>
		public virtual bool AllowRemergingExecConnections => true;

		/// <summary>
		/// Global index of this node in the graph. Each node in a graph has a unique index.
		/// </summary>
		public int GraphIndex { get; set; } = -1;

		public IEnumerable<UndefinedGenericType> GetUndefinedGenericTypes() => InputsAndOutputs.SelectMany(x => x.Type.GetUndefinedGenericTypes()).Distinct();

		public record class AlternateOverload(TypeBase ReturnType, List<IMethodParameterInfo> Parameters);
		public virtual IEnumerable<AlternateOverload> AlternatesOverloads => [];

		/// <summary>
		/// returns a list of changed connections, if any
		/// </summary>
		/// <param name="connection">The connection that was generic, it is not generic anymore</param>
		public virtual List<Connection> GenericConnectionTypeDefined(UndefinedGenericType previousType, Connection connection, TypeBase baseType)
		{
			return [];
		}

		public virtual void SelectOverload(AlternateOverload overload, out List<Connection> newConnections, out List<Connection> removedConnections)
		{
			throw new NotImplementedException();
		}

		internal virtual Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info) => throw new NotImplementedException();

		internal virtual void BuildInlineExpression(BuildExpressionInfo info) => throw new NotImplementedException();

		/// <summary>
		/// Create an Expression node that can be used in the graph.
		/// Ie, the "Add" node will have two local variables, one for each input, and one output local variable.
		/// In C#, that would look like doing : 
		/// var input1 = ..., input2 = ...; // ... would be replaced by the output local variable of the previous node
		/// var output = input1 + input2;
		/// 
		/// This will ignore the exec connections.
		/// </summary>
		internal virtual IEnumerable<(Connection Connection, ParameterExpression LocalVariable)> CreateOutputsLocalVariableExpressions(BuildExpressionInfo info)
		{
			foreach (var output in Outputs)
			{
				if (output.Type.IsExec)
					continue;

				var variable = Expression.Variable(output.Type.MakeRealType(), $"{Name}_{output.Name}"
					.Replace(" ", string.Empty)
					.Replace(".", string.Empty)
					.Replace("<", string.Empty)
					.Replace(">", string.Empty));
				yield return (output, variable);
			}
		}

		#region Path merging / Crossing examinations

		public abstract string GetExecOutputPathId(string pathId, Connection execOutput);

		public abstract bool DoesOutputPathAllowDeadEnd(Connection execOutput);

		public abstract bool DoesOutputPathAllowMerge(Connection execOutput);

        /// <summary>
        /// Returns true if this node breaks a dead end. These are usually "Return", "Break", "Continue", etc.
        /// This will allow a dead end in places where it shouldn't be allowed, such as a "Branch" node.
        /// </summary>
        public virtual bool BreaksDeadEnd => false;

		public class InfiniteLoopException(Node node) : Exception
		{
			public Node Node { get; } = node;
		}

		/// <summary>
		/// Returns all the possible execution path from this node.
		/// I know, I know. All of this is slow.
		/// </summary>
		public NodePaths SearchAllExecPaths(HashSet<Node> nodesBefore)
		{
			if (nodesBefore.Contains(this))
				throw new InfiniteLoopException(this);

			var execOutputs = Outputs.Where(x => x.Type.IsExec).ToList();
			if (execOutputs.Count == 0)
				return new();

			// Prevent any children path from looping back to us
			nodesBefore.Add(this);

			var allPaths = new NodePaths();

			foreach (var execOutput in execOutputs)
			{
				// Create a copy of the nodes already used since each execOutput can possibly hit the same nodes without it being an issue
				var nodesBefore_local = new HashSet<Node>(nodesBefore);

				var subPaths = SearchAllExecPaths(execOutput, nodesBefore_local);

				if (subPaths.CountPossiblePaths != 0) // it led somewhere
				{
					// Add the execOutput to the beginning of each path
					subPaths.PrependPath(new([execOutput]));
				}
				else // it led nowhere, the path is just the execOutput and that's it
					subPaths.AddNewIndependantBranch(new NodePath([execOutput]));

				allPaths.AddNewIndependantBranch(subPaths);
			}

			return allPaths;
		}

		/// <summary>
		/// List all the possible path taken from the output exec connection.
		/// </summary>
		private static NodePaths SearchAllExecPaths(Connection outputExec, HashSet<Node> alreadySeenNodes)
		{
			if (outputExec.Connections.Count == 0)
				return new(); // we're connected to nothing

			// If we are going through NormalFlowNodes, we can simply accumulate the connections one after the other
			// Then, we multiple paths offers, we can search those paths and Prepend the straightConnections to them
			var straightConnections = new NodePath();

			var inputExec = outputExec.Connections.FirstOrDefault();
			while (inputExec != null)
			{
				var otherNode = inputExec.Parent;

				if (otherNode is NormalFlowNode)
				{
					alreadySeenNodes.Add(otherNode); // used to prevent any children path from looping back to us

					var otherExec = otherNode.Outputs.Single(x => x.Type.IsExec);
					straightConnections.AppendPath(otherExec);

					inputExec = otherExec.Connections.FirstOrDefault();
				}
				else
				{
					// We've hit a possible separation in the paths we can follow
					// We can recursively start a search again.
					// There is no need to add the current node to the alreadySeenNodes since the SearchAllExecPaths will do it for us
					var allSubPaths = otherNode.SearchAllExecPaths(alreadySeenNodes);

					// Append each path returned to the straightConnections
					allSubPaths.PrependPath(straightConnections);

					return allSubPaths;
				}
			}

			// if we're here, it means we never hit any branching of paths, we can return a single path possible
			if (straightConnections.Length == 0)
				return new(); // we're connected to nothing
			else
				return new NodePaths(straightConnections);
		}


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

		public record SerializedNode(string Type, string Id, string Name, List<Connection.SerializedConnection> Inputs, List<Connection.SerializedConnection> Outputs, Dictionary<string, string> Decorations);
		internal SerializedNode Serialize()
		{
			var serializedNode = new SerializedNode(GetType().FullName!, Id, Name, Inputs.Select(x => x.Serialize()).ToList(), Outputs.Select(x => x.Serialize()).ToList(), Decorations.ToDictionary(x => x.Key.FullName!, x => x.Value.Serialize()));

			return serializedNode;
		}

		internal static Node Deserialize(Graph graph, SerializedNode serializedNodeObj)
		{
			var type = graph.SelfClass.TypeFactory.GetTypeByFullName(serializedNodeObj.Type) ?? throw new Exception($"Unable to find type: {serializedNodeObj.Type}");
			var node = (Node?)Activator.CreateInstance(type, graph, serializedNodeObj.Id) ?? throw new Exception($"Unable to create instance of type: {serializedNodeObj.Type}");

			foreach (var decoration in serializedNodeObj.Decorations)
			{
				var decorationType = graph.SelfClass.TypeFactory.GetTypeByFullName(decoration.Key) ?? throw new Exception($"Unable to find type: {decoration.Key}");

				var method = decorationType.GetMethod(nameof(INodeDecoration.Deserialize), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

				if (method == null)
					throw new Exception($"Unable to find Deserialize method on type: {decoration.Key}");

				var decorationObj = method.Invoke(null, new object[] { graph.SelfClass.TypeFactory, decoration.Value }) as INodeDecoration;

				if (decorationObj == null)
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
				var connection = Connection.Deserialize(this, input, true);
				Inputs.Add(connection);
			}

			foreach (var output in serializedNodeObj.Outputs)
			{
				var connection = Connection.Deserialize(this, output, false);
				Outputs.Add(connection);
			}
		}


		#endregion
	}
}
