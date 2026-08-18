using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using NodeDev.Blazor.DiagramsModels;
using NodeDev.Core.Nodes.Delegates;

namespace NodeDev.Tests;

public class GraphPortModelTests
{
	[Fact]
	public void ImplicitReturnCanBeProjectedAsAGroupPort()
	{
		var graph = GraphExecutorTests.CreateSimpleAddGraph<int, int>(out _, out _, out _);
		var func = (CreateFuncNode)graph.Manager.AddNode(
			new NodeDev.Core.NodeProvider.NodeSearchResult(typeof(CreateFuncNode)),
			_ => { },
			callableScopeId: null);
		func.SetResultType(graph.Project.TypeFactory.Get<int>());
		func.AddParameter("value", graph.Project.TypeFactory.Get<int>());

		var entry = Assert.Single(graph.GetNodesInScope(func.BodyScopeId).OfType<LambdaEntryNode>());
		var lambdaReturn = Assert.Single(graph.GetNodesInScope(func.BodyScopeId).OfType<LambdaReturnNode>());
		var group = new LambdaGroupModel(func);
		var sourceNode = new GraphNodeModel(entry);
		var sourcePort = new GraphPortModel(sourceNode, entry.ParameterOutputs[0], isInput: false);
		var destinationPort = new GraphPortModel(group, lambdaReturn.ResultInput, isInput: true);

		Assert.Same(lambdaReturn, group.BoundaryReturn);
		Assert.Equal(LambdaGroupModel.FuncPadding, group.Padding);
		Assert.Equal(LambdaGroupModel.MinimumWidth, group.Size!.Width);
		Assert.Equal(LambdaGroupModel.MinimumHeight, group.Size.Height);
		Assert.Same(group, destinationPort.Parent);
		Assert.True(sourcePort.CanAttachTo(destinationPort));
		Assert.True(destinationPort.CanAttachTo(sourcePort));
	}

	[Fact]
	public void MinimumSizeGroup_KeepsWorkspaceWhileChildrenMoveInsideIt()
	{
		var graph = GraphExecutorTests.CreateSimpleAddGraph<int, int>(out _, out _, out _);
		var func = (CreateFuncNode)graph.Manager.AddNode(
			new NodeDev.Core.NodeProvider.NodeSearchResult(typeof(CreateFuncNode)),
			_ => { },
			callableScopeId: null);
		var group = new LambdaGroupModel(func);
		var child = new NodeModel(new Point(180, 180)) { Size = new Size(100, 80) };

		group.AddChild(child);
		child.SetPosition(400, 250);

		Assert.Equal(0, group.Position.X);
		Assert.Equal(0, group.Position.Y);
		Assert.Equal(600, group.Size!.Width);
		Assert.Equal(420, group.Size.Height);

		child.SetPosition(520, 250);

		Assert.Equal(710, group.Size.Width);
		Assert.Equal(420, group.Size.Height);
	}

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
