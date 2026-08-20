using Blazor.Diagrams;
using NodeDev.Blazor.Components;
using NodeDev.Core;
using NodeDev.Core.Nodes;

namespace NodeDev.Tests;

public class GraphDiagramSynchronizationTests
{
	[Fact]
	public void MultipleProjectionsSubscribeToTheSameGraphIndependently()
	{
		Project.CreateNewDefaultProject(out var main);
		var first = CreateProjection(main.Graph);
		var second = CreateProjection(main.Graph);
		var initialNodeCount = first.Diagram.Nodes.Count;

		var firstNode = new MethodCall(main.Graph);
		main.Graph.Manager.AddNode(firstNode);

		Assert.Equal(initialNodeCount + 1, first.Diagram.Nodes.Count);
		Assert.Equal(initialNodeCount + 1, second.Diagram.Nodes.Count);

		first.Synchronizer.Dispose();
		var secondNode = new MethodCall(main.Graph);
		main.Graph.Manager.AddNode(secondNode);

		Assert.Equal(initialNodeCount + 1, first.Diagram.Nodes.Count);
		Assert.Equal(initialNodeCount + 2, second.Diagram.Nodes.Count);

		second.Synchronizer.Dispose();
	}

	[Fact]
	public void DomainConnectionReplacementIsAppliedToProjection()
	{
		Project.CreateNewDefaultProject(out var main);
		var projection = CreateProjection(main.Graph);
		Assert.Single(projection.Diagram.Links);

		var methodCall = new MethodCall(main.Graph);
		main.Graph.Manager.AddNode(methodCall);
		main.Graph.Manager.AddNewConnectionBetween(main.EntryNode!.Outputs[0], methodCall.Inputs[0]);

		var link = Assert.Single(projection.Diagram.Links);
		Assert.Equal(main.EntryNode.Outputs[0], projection.Projection.FindPort(main.EntryNode.Outputs[0])?.Connection);
		Assert.Equal(methodCall.Inputs[0], projection.Projection.FindPort(methodCall.Inputs[0])?.Connection);
		Assert.Equal(main.EntryNode.Outputs[0], ((NodeDev.Blazor.DiagramsModels.GraphPortModel?)link.Source.Model)?.Connection);
		Assert.Equal(methodCall.Inputs[0], ((NodeDev.Blazor.DiagramsModels.GraphPortModel?)link.Target.Model)?.Connection);

		projection.Synchronizer.Dispose();
	}

	[Fact]
	public void ProjectionResetRebuildsFromCurrentDomainState()
	{
		Project.CreateNewDefaultProject(out var main);
		var projection = CreateProjection(main.Graph);
		projection.Diagram.Nodes.Clear();
		Assert.Empty(projection.Diagram.Nodes);

		main.Graph.RaiseGraphChanged(true);

		Assert.Equal(main.Graph.Nodes.Count, projection.Diagram.Nodes.Count);
		projection.Synchronizer.Dispose();
	}

	private static ProjectionFixture CreateProjection(Graph graph)
	{
		var diagram = new BlazorDiagram();
		var synchronizer = new GraphDiagramSynchronizer(graph, action =>
		{
			action();
			return Task.CompletedTask;
		});
		var projection = new GraphDiagramProjection(graph, diagram, synchronizer, (_, _) => { });
		projection.Initialize();
		synchronizer.Start(projection, () => { });
		return new(diagram, projection, synchronizer);
	}

	private sealed record ProjectionFixture(
		BlazorDiagram Diagram,
		GraphDiagramProjection Projection,
		GraphDiagramSynchronizer Synchronizer);
}
