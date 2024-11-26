using NodeDev.Core.ManagerServices;
using NodeDev.Core;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using NSubstitute;

namespace NodeDev.Tests;

public class GraphManagerServiceTests : NodeDevTestsBase
{
	[Fact]
	public void ConnectTwoExecInOneOutput_ShouldDisconnectFirstExec()
	{
		var project = Project.CreateNewDefaultProject(out var main);
		Assert.NotNull(main.EntryNode);
		Assert.Single(main.ReturnNodes);
		Assert.Equal(main.EntryNode.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);

		// create fake IGraphCanvas
		var graphCanvas = Substitute.For<IGraphCanvas>();
		graphCanvas.Graph.Returns(main.Graph);

		var graphManager = new GraphManagerService(graphCanvas);

		// create a random method call used to test the connection
		var methodCall = new MethodCall(main.Graph);
		main.Graph.AddNode(methodCall, false);


		// This should also disconnect the entry node's existing exec connection
		graphManager.AddNewConnectionBetween(main.EntryNode.Outputs[0], methodCall.Inputs[0]);

		// main entry node was disconnected from the other node and is now connected to the method call
		Assert.Single(main.EntryNode.Outputs[0].Connections);
		Assert.Equal(main.EntryNode.Outputs[0].Connections[0], methodCall.Inputs[0]);

		// return node is not connected to anything
		Assert.Empty(main.ReturnNodes.Single().Inputs[0].Connections);

		// check that each connection was updated
		graphCanvas.Received().UpdatePortColor(Arg.Is(main.EntryNode.Outputs[0].Connections[0]));
		graphCanvas.Received().UpdatePortColor(Arg.Is(methodCall.Inputs[0]));
		graphCanvas.Received().UpdatePortColor(Arg.Is(main.ReturnNodes.Single().Inputs[0]));

		// check that the old connection was removed from the graph canvas
		graphCanvas.Received().RemoveLinkFromGraphCanvas(Arg.Is(main.EntryNode.Outputs[0]), Arg.Is(main.ReturnNodes.Single().Inputs[0]));
	}

	[Fact]
	public void ConnectTwoOutputsInOneInput_ShouldDisconnectFirstOutput()
	{
		var project = Project.CreateNewDefaultProject(out var main);
		Assert.NotNull(main.EntryNode);
		Assert.Single(main.ReturnNodes);
		Assert.Equal(main.EntryNode.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);

		// create fake IGraphCanvas
		var graphCanvas = Substitute.For<IGraphCanvas>();
		graphCanvas.Graph.Returns(main.Graph);

		var graphManager = new GraphManagerService(graphCanvas);

		var addNode1 = AddNewAddNodeToGraph<int>(main.Graph);
		var addNode2 = AddNewAddNodeToGraph<int>(main.Graph);
		var addNode3 = AddNewAddNodeToGraph<int>(main.Graph);

		// connect output of addNode1 to input of addNode3
		graphManager.AddNewConnectionBetween(addNode1.Outputs[0], addNode3.Inputs[0]);
		graphCanvas.Received().UpdatePortColor(Arg.Is(addNode1.Outputs[0]));
		graphCanvas.Received().UpdatePortColor(Arg.Is(addNode3.Inputs[0]));
		Assert.Single(addNode1.Outputs[0].Connections);
		Assert.Single(addNode3.Inputs[0].Connections);
		Assert.Equal(addNode1.Outputs[0].Connections[0], addNode3.Inputs[0]);

		// connect output of addNode2 to input of addNode3. It should disconnect the existing connection
		graphManager.AddNewConnectionBetween(addNode2.Outputs[0], addNode3.Inputs[0]);
		graphCanvas.Received(2).UpdatePortColor(Arg.Is(addNode1.Outputs[0])); // when first adding, then when disconnecting
		graphCanvas.Received(1).UpdatePortColor(Arg.Is(addNode2.Outputs[0]));
		graphCanvas.Received(3).UpdatePortColor(Arg.Is(addNode3.Inputs[0])); // when first adding, adding a second time, then disconnecting
		Assert.Empty(addNode1.Outputs[0].Connections);
		Assert.Single(addNode2.Outputs[0].Connections);
		Assert.Single(addNode3.Inputs[0].Connections);
		Assert.Equal(addNode2.Outputs[0].Connections[0], addNode3.Inputs[0]);

		graphCanvas.Received().RemoveLinkFromGraphCanvas(Arg.Is(addNode1.Outputs[0]), Arg.Is(addNode3.Inputs[0]));
	}

	[Fact]
	public void ConnectTwoExecOutputsInOneInput_ShouldAllow()
	{
		var project = Project.CreateNewDefaultProject(out var main);
		Assert.NotNull(main.EntryNode);
		Assert.Single(main.ReturnNodes);
		Assert.Equal(main.EntryNode.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);

		// create fake IGraphCanvas
		var graphCanvas = Substitute.For<IGraphCanvas>();
		graphCanvas.Graph.Returns(main.Graph);

		var graphManager = new GraphManagerService(graphCanvas);

		// create a random method call used to test the connection
		var methodCall = new MethodCall(main.Graph);
		main.Graph.AddNode(methodCall, false);

		// connect output of addNode1 to input of addNode3
		graphManager.AddNewConnectionBetween(methodCall.Outputs[0], main.ReturnNodes.Single().Inputs[0]);
		graphCanvas.Received().UpdatePortColor(Arg.Is(methodCall.Outputs[0]));
		graphCanvas.Received().UpdatePortColor(Arg.Is(main.ReturnNodes.Single().Inputs[0]));
		Assert.Single(main.EntryNode.Outputs[0].Connections);
		Assert.Equal(2, main.ReturnNodes.Single().Inputs[0].Connections.Count);
		Assert.Single(methodCall.Outputs[0].Connections);
		Assert.Equal(methodCall.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);
		graphCanvas.DidNotReceiveWithAnyArgs().RemoveLinkFromGraphCanvas(Arg.Any<Connection>(), Arg.Any<Connection>());
	}

	[Fact]
	public void ConnectArrayToIEnumerableT_ShouldAllow()
	{
		var project = Project.CreateNewDefaultProject(out var main);
		Assert.NotNull(main.EntryNode);
		Assert.Single(main.ReturnNodes);
		Assert.Equal(main.EntryNode.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);

		var typeFactory = main.TypeFactory;

		// create fake IGraphCanvas
		var graphCanvas = Substitute.For<IGraphCanvas>();
		graphCanvas.Graph.Returns(main.Graph);

		var graphManager = new GraphManagerService(graphCanvas);

		// create a random method call used to test the connection
		var methodCall = AddMethodCall(main.Graph, typeFactory.Get<Array>(), nameof(Array.Empty));
		methodCall.Outputs[1].UpdateTypeAndTextboxVisibility(typeFactory.Get<string[]>(), overrideInitialType: true);

		var foreachNode = new ForeachNode(main.Graph);
		main.Graph.AddNode(foreachNode, false);

		// connect output of Array.Empty<string>() to input of foreachNode
		graphManager.AddNewConnectionBetween(methodCall.Outputs[1], foreachNode.Inputs[1]);
		graphCanvas.Received().UpdatePortColor(Arg.Is(methodCall.Outputs[1]));
		graphCanvas.Received().UpdatePortColor(Arg.Is(foreachNode.Inputs[1]));
		graphCanvas.Received().UpdatePortColor(Arg.Is(foreachNode.Outputs[1]));
		Assert.Equal(typeFactory.Get<IEnumerable<string>>(), foreachNode.Inputs[1].Type);
		Assert.Equal(typeFactory.Get<string>(), foreachNode.Outputs[1].Type);
	}

	[Fact]
	public void ConnectListArrayToForeach_ShouldPropagateChange()
	{
		var project = Project.CreateNewDefaultProject(out var main);
		Assert.NotNull(main.EntryNode);
		Assert.Single(main.ReturnNodes);
		Assert.Equal(main.EntryNode.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);

		var typeFactory = main.TypeFactory;

		// create fake IGraphCanvas
		var graphCanvas = Substitute.For<IGraphCanvas>();
		graphCanvas.Graph.Returns(main.Graph);

		var graphManager = new GraphManagerService(graphCanvas);

		// create a random method call used to test the connection
		var newListArray = new New(main.Graph);
		newListArray.Outputs[1].UpdateTypeAndTextboxVisibility(typeFactory.Get<List<string[]>>(), overrideInitialType: true);
		newListArray.GenericConnectionTypeDefined(newListArray.Outputs[1]);

		var foreachNode = new ForeachNode(main.Graph);
		main.Graph.AddNode(foreachNode, false);

		var foreachNode2 = new ForeachNode(main.Graph);
		main.Graph.AddNode(foreachNode2, false);

		// connect output of foreachNode into input of foreachNode2
		graphManager.AddNewConnectionBetween(foreachNode.Outputs[1], foreachNode2.Inputs[1]);

		// connect output of new List<string[]> to input of foreachNode
		graphManager.AddNewConnectionBetween(newListArray.Outputs[1], foreachNode.Inputs[1]);
		graphCanvas.Received().UpdatePortColor(Arg.Is(newListArray.Outputs[1]));
		graphCanvas.Received().UpdatePortColor(Arg.Is(foreachNode.Inputs[1]));
		graphCanvas.Received().UpdatePortColor(Arg.Is(foreachNode.Outputs[1]));
		graphCanvas.Received().UpdatePortColor(Arg.Is(foreachNode2.Inputs[1]));
		graphCanvas.Received().UpdatePortColor(Arg.Is(foreachNode2.Outputs[1]));

		// Input of foreach node should be IEnumerable<string[]>, output should be string[]
		Assert.Equal(typeFactory.Get<IEnumerable<string[]>>(), foreachNode.Inputs[1].Type);
		Assert.Equal(typeFactory.Get<string[]>(), foreachNode.Outputs[1].Type);

		// Input of foreach node 2 should be string[], output should be string
		Assert.Equal(typeFactory.Get<IEnumerable<string>>(), foreachNode2.Inputs[1].Type);
		Assert.Equal(typeFactory.Get<string>(), foreachNode2.Outputs[1].Type);
	}

	[Fact]
	public void ConnectArrayToArrayT_ShouldPropagateChange()
	{
		var project = Project.CreateNewDefaultProject(out var main);
		Assert.NotNull(main.EntryNode);
		Assert.Single(main.ReturnNodes);
		Assert.Equal(main.EntryNode.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);

		var typeFactory = main.TypeFactory;

		// create fake IGraphCanvas
		var graphCanvas = Substitute.For<IGraphCanvas>();
		graphCanvas.Graph.Returns(main.Graph);

		var graphManager = new GraphManagerService(graphCanvas);

		// output string[]
		var newArray = new New(main.Graph);
		newArray.Outputs[1].UpdateTypeAndTextboxVisibility(typeFactory.Get<string[]>(), overrideInitialType: true);
		newArray.GenericConnectionTypeDefined(newArray.Outputs[1]);

		var arrayGet = new ArrayGet(main.Graph);
		main.Graph.AddNode(arrayGet, false);

		// connect output of foreachNode into input of foreachNode2
		graphManager.AddNewConnectionBetween(newArray.Outputs[1], arrayGet.Inputs[0]);

		graphCanvas.Received().UpdatePortColor(Arg.Is(newArray.Outputs[1]));
		graphCanvas.Received().UpdatePortColor(Arg.Is(arrayGet.Inputs[0]));
		graphCanvas.Received().UpdatePortColor(Arg.Is(arrayGet.Outputs[0]));

		// Input of arrayGet should be string[], output should be string
		Assert.Equal(typeFactory.Get<string[]>(), arrayGet.Inputs[0].Type);
		Assert.Equal(typeFactory.Get<string>(), arrayGet.Outputs[0].Type);
	}
}
