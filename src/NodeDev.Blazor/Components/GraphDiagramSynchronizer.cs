using NodeDev.Core;
using System.Reactive.Linq;

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
	private IDisposable? GraphChangedSubscription;
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
	/// Starts listening for UI-relevant domain changes. Bursts are sampled to avoid repeatedly rebuilding
	/// the diagram while one logical graph operation emits several notifications.
	/// </summary>
	public void Start(Action rebuildProjection, Action stateHasChanged)
	{
		GraphChangedSubscription = Graph.Project.GraphChanged
			.Where(change => change.RequireUIRefresh && change.Graph == Graph)
			.AcceptThenSample(TimeSpan.FromMilliseconds(250))
			.Subscribe(change =>
			{
				_ = InvokeAsync(() =>
				{
					rebuildProjection();
					stateHasChanged();
				});
			});
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
		GraphChangedSubscription?.Dispose();
		GraphChangedSubscription = null;
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
