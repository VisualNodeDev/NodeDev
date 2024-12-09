using NodeDev.Core;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;

namespace NodeDev.Blazor.Services;

/// <summary>
/// Utility service used to get the currently debugged path through the tree of executors
/// </summary>
internal class DebuggedPathService
{
	/// <summary>
	/// The stack of nodes throught which we are debugging. 
	/// Ie, if we are debugging Main -> MethodCall1 -> MethodCall2, this will contain MethodCall1, MethodCall2. Or more the actual nodes of each of them
	/// </summary>
	private readonly List<Node> GraphIndexesAndNodes_ = [];

	/// <summary>
	/// The queue of nodes throught which we are debugging. 
	/// Ie, if we are debugging Main -> MethodCall1 -> MethodCall2, this will contain MethodCall1, MethodCall2. Or more the actual nodes of each of them
	/// </summary>
	public IReadOnlyCollection<Node> GraphNodes => GraphIndexesAndNodes_;

	private Project? Project;

	public delegate void DebuggedPathChangedHandler();
	public event DebuggedPathChangedHandler? DebuggedPathChanged;

	public GraphExecutor? GraphExecutor
	{
		get
		{
			if (Project == null)
				return null;

			var executor = Project.GraphExecutor;

			foreach (var node in GraphIndexesAndNodes_)
			{
				if (executor == null)
					return null;

				executor = executor.GetChildrenExecutor(node.GraphIndex);
			}

			return executor;
		}
	}

	public string GetDebugValue(Connection connection)
	{
		var executor = GraphExecutor;

		// Make sure the executor is not null
		if (executor == null)
			return connection.Type.FriendlyName;

		var value = executor.Connections[connection.GraphIndex];

		return value?.ToString() ?? "null";
	}

	public void ChangeProject(Project project)
	{
		GraphIndexesAndNodes_.Clear();
		Project = project;

		DebuggedPathChanged?.Invoke();
	}

	public void EnterExecutor(Node node)
	{
		GraphIndexesAndNodes_.Add(node);

		DebuggedPathChanged?.Invoke();
	}

	public void ExitExecutor(int amountOfIndexesToKeep)
	{
		GraphIndexesAndNodes_.RemoveRange(amountOfIndexesToKeep, GraphIndexesAndNodes_.Count - amountOfIndexesToKeep);

		DebuggedPathChanged?.Invoke();
	}
}
