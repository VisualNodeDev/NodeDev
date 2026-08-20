using NodeDev.Core;

namespace NodeDev.Blazor.Components;

/// <summary>
/// Coordinates graph-to-diagram refreshes and prevents programmatic diagram changes from flowing back
/// into the domain as if they were user edits. Suppression uses counters so nested operations restore
/// the previous state correctly when their disposable scopes exit.
/// </summary>
internal sealed class GraphDiagramSynchronizer : IDisposable
{
	private readonly Graph Graph;
	private readonly Func<Action, Task> InvokeAsync;
	private IDisposable? GraphChangeSubscription;
	private int ConnectionSuppressionCount;
	private int NodeRemovalSuppressionCount;
	private int VertexLoadingCount;

	/// <summary>
	/// Creates a synchronizer for a single graph. <paramref name="invokeAsync"/> must marshal callbacks
	/// onto the owning Blazor component's renderer context.
	/// </summary>
	public GraphDiagramSynchronizer(Graph graph, Func<Action, Task> invokeAsync)
	{
		Graph = graph;
		InvokeAsync = invokeAsync;
	}

	public bool IsConnectionUpdateSuppressed => ConnectionSuppressionCount != 0;
	public bool IsNodeRemovalSuppressed => NodeRemovalSuppressionCount != 0;
	public bool IsLoadingVertices => VertexLoadingCount != 0;

	/// <summary>
	/// Starts listening for domain changes and applies them to this synchronizer's diagram projection.
	/// </summary>
	public void Start(GraphDiagramProjection projection, Action stateHasChanged)
	{
		GraphChangeSubscription = Graph.Changes
			.Subscribe(change =>
			{
				_ = InvokeAsync(() =>
				{
					ApplyChange(projection, change);
					stateHasChanged();
				});
			});
	}

	private static void ApplyChange(GraphDiagramProjection projection, GraphChange change)
	{
		switch (change)
		{
			case GraphChange.NodeAdded added:
				projection.AddNode(added.Node);
				break;
			case GraphChange.NodeRemoved removed:
				projection.RemoveNode(removed.Node);
				break;
			case GraphChange.NodeChanged changed:
				projection.Refresh(changed.Node);
				break;
			case GraphChange.LinkAdded added:
				projection.AddLink(added.Source, added.Destination);
				break;
			case GraphChange.LinkRemoved removed:
				projection.RemoveLink(removed.Source, removed.Destination);
				break;
			case GraphChange.ConnectionChanged changed:
				projection.RefreshConnection(changed.Connection);
				break;
			case GraphChange.ProjectionReset:
				projection.Rebuild();
				break;
		}
	}

	/// <summary>
	/// Prevents diagram link callbacks from adding or removing domain connections during programmatic updates.
	/// </summary>
	public IDisposable SuppressConnectionUpdates() => Suppress(
		() => ConnectionSuppressionCount++,
		() => ConnectionSuppressionCount--);

	/// <summary>
	/// Prevents diagram node-removal callbacks from deleting domain nodes during programmatic updates.
	/// </summary>
	public IDisposable SuppressNodeRemovals() => Suppress(
		() => NodeRemovalSuppressionCount++,
		() => NodeRemovalSuppressionCount--);

	/// <summary>
	/// Marks link vertices as being restored from saved domain data instead of being created by the user.
	/// </summary>
	public IDisposable SuppressVertexLoading() => Suppress(
		() => VertexLoadingCount++,
		() => VertexLoadingCount--);

	/// <summary>
	/// Suppresses both connection and node-removal callbacks for bulk projection operations such as rebuilds.
	/// </summary>
	public IDisposable SuppressCanvasMutations()
	{
		ConnectionSuppressionCount++;
		NodeRemovalSuppressionCount++;
		return new CallbackScope(() =>
		{
			NodeRemovalSuppressionCount--;
			ConnectionSuppressionCount--;
		});
	}

	/// <summary>
	/// Enters a suppression state and returns an idempotent scope that restores it on disposal.
	/// </summary>
	private static IDisposable Suppress(Action enter, Action exit)
	{
		enter();
		return new CallbackScope(exit);
	}

	/// <summary>
	/// Stops graph notifications from reaching the diagram after the owning canvas is disposed.
	/// </summary>
	public void Dispose()
	{
		GraphChangeSubscription?.Dispose();
		GraphChangeSubscription = null;
	}

	/// <summary>
	/// Runs a restoration callback at most once, allowing suppression scopes to be safely disposed repeatedly.
	/// </summary>
	private sealed class CallbackScope(Action callback) : IDisposable
	{
		private Action? Callback = callback;

		public void Dispose()
		{
			Interlocked.Exchange(ref Callback, null)?.Invoke();
		}
	}
}
