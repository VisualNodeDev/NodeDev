using Blazor.Diagrams;
using NodeDev.Blazor.DiagramsModels;
using NodeDev.Core;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using System.Reactive.Linq;

namespace NodeDev.Blazor.Components;

/// <summary>
/// Projects graph execution notifications onto the diagram as short-lived node/connection animations.
/// Events are filtered to this visualizer's graph and buffered to reduce renderer work during rapid execution.
/// </summary>
internal sealed class GraphDebugVisualizer : IDisposable
{
	private readonly Graph Graph;
	private readonly BlazorDiagram Diagram;
	private readonly Func<Action, Task> InvokeAsync;
	private IDisposable? NodeExecutingSubscription;

	/// <summary>
	/// Creates a visualizer for one graph and its diagram. <paramref name="invokeAsync"/> must dispatch
	/// diagram updates through the owning Blazor component.
	/// </summary>
	public GraphDebugVisualizer(Graph graph, BlazorDiagram diagram, Func<Action, Task> invokeAsync)
	{
		Graph = graph;
		Diagram = diagram;
		InvokeAsync = invokeAsync;
	}

	/// <summary>
	/// Starts observing execution events. Events are buffered briefly and dispatched as one renderer update.
	/// </summary>
	public void Start()
	{
		NodeExecutingSubscription = Graph.Project.GraphNodeExecuting
			.Where(change => change.Executor.Graph == Graph)
			.Buffer(TimeSpan.FromMilliseconds(250))
			.Where(changes => changes.Count != 0)
			.Subscribe(changes =>
			{
				_ = InvokeAsync(() => ShowExecutingConnections(changes));
			});
	}

	/// <summary>
	/// Finds the projected model for each executed node and starts its animation. Duplicate execution
	/// connections within the same buffer are collapsed, and removed/unprojected nodes are ignored.
	/// </summary>
	private void ShowExecutingConnections(IList<(GraphExecutor Executor, Node Node, Connection Exec)> changes)
	{
		foreach (var change in changes.DistinctBy(item => item.Exec))
		{
			var nodeModel = Diagram.Nodes.OfType<GraphNodeModel>().FirstOrDefault(model => model.Node == change.Node);
			if (nodeModel == null)
				continue;

			_ = nodeModel.OnNodeExecuting(change.Exec);
		}
	}

	/// <summary>
	/// Stops execution events from updating the diagram after its canvas has been disposed.
	/// </summary>
	public void Dispose()
	{
		NodeExecutingSubscription?.Dispose();
		NodeExecutingSubscription = null;
	}
}
