using NodeDev.Blazor.DiagramsModels;
using NodeDev.Core.Nodes.Delegates;

namespace NodeDev.Tests;

public class GraphPortModelTests
{
	[Fact]
	public void DataOutputCanAttachFromEnclosingScopeIntoLambda()
	{
		var graph = GraphExecutorTests.CreateSimpleAddGraph<int, int>(out var entry, out _, out _);
		var func = (CreateFuncNode)graph.Manager.AddNode(
			new NodeDev.Core.NodeProvider.NodeSearchResult(typeof(CreateFuncNode)),
			_ => { },
			callableScopeId: null);
		func.SetResultType(graph.Project.TypeFactory.Get<int>());
		var lambdaReturn = Assert.Single(graph.GetNodesInScope(func.BodyScopeId).OfType<LambdaReturnNode>());

		var sourceNode = new GraphNodeModel(entry);
		var destinationNode = new GraphNodeModel(lambdaReturn);
		var sourcePort = new GraphPortModel(sourceNode, entry.Outputs[1], isInput: false);
		var destinationPort = new GraphPortModel(destinationNode, lambdaReturn.ResultInput, isInput: true);

		Assert.True(sourcePort.CanAttachTo(destinationPort));
		Assert.True(destinationPort.CanAttachTo(sourcePort));
	}
}
