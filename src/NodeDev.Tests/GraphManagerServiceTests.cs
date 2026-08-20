using NodeDev.Core;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using System.Reactive.Linq;

namespace NodeDev.Tests;

public class GraphManagerServiceTests : NodeDevTestsBase
{
	[Fact]
	public void SelectingNewListOverloadRefreshesPortsAndKeepsExecConnected()
	{
		var project = Project.CreateNewDefaultProject(out var main);
		var changes = new List<GraphChange>();
		using var subscription = main.Graph.Changes.Subscribe(changes.Add);

		var newNode = new New(main.Graph);
		main.Graph.Manager.AddNode(newNode);
		main.Graph.Manager.AddNewConnectionBetween(main.EntryNode!.Outputs[0], newNode.Inputs[0]);
		main.Graph.Manager.PropagateNewGeneric(
			newNode,
			new Dictionary<string, NodeDev.Core.Types.TypeBase>
			{
				["T"] = project.TypeFactory.Get<List<string>>()
			},
			useInitialTypes: false,
			initiatingConnection: null,
			overrideInitialTypes: true);

		var capacityOverload = Assert.Single(
			newNode.AlternatesOverloads,
			overload => overload.Parameters.Count == 1 && overload.Parameters[0].Name == "capacity");
		changes.Clear();

		main.Graph.Manager.SelectNodeOverload(newNode, capacityOverload);

		Assert.Contains(main.EntryNode.Outputs[0], newNode.Inputs[0].Connections);
		Assert.Equal("capacity", newNode.Inputs[1].Name);
		Assert.Contains(changes, change => change is GraphChange.NodeChanged nodeChanged && nodeChanged.Node == newNode);
	}

	[Fact]
	public void AddEnumerableRange_ShouldExposeAllPortsWhenNodeAddedIsPublished()
	{
		var project = Project.CreateNewDefaultProject(out var main);

		string[]? inputsSeenBySubscriber = null;
		string[]? outputsSeenBySubscriber = null;
		using var subscription = main.Graph.Changes.Subscribe(change =>
		{
			if (change is GraphChange.NodeAdded added)
			{
				inputsSeenBySubscriber = added.Node.Inputs.Select(connection => connection.Name).ToArray();
				outputsSeenBySubscriber = added.Node.Outputs.Select(connection => connection.Name).ToArray();
			}
		});

		var rangeSearchResult = Assert.Single(
			NodeProvider.Search(main.Graph, "Enumerable.Range", null, null)
				.OfType<NodeProvider.MethodCallNode>());

		var rangeNode = Assert.IsType<MethodCall>(main.Graph.Manager.AddNode(rangeSearchResult, _ => { }));

		Assert.NotNull(inputsSeenBySubscriber);
		Assert.NotNull(outputsSeenBySubscriber);
		Assert.Equal(["Exec", "start", "count"], inputsSeenBySubscriber);
		Assert.Equal(["Exec", "Result"], outputsSeenBySubscriber);
		Assert.Equal(inputsSeenBySubscriber, rangeNode.Inputs.Select(connection => connection.Name));
		Assert.Equal(outputsSeenBySubscriber, rangeNode.Outputs.Select(connection => connection.Name));
	}

	[Fact]
	public void ConnectTwoExecInOneOutput_ShouldDisconnectFirstExec()
	{
		var project = Project.CreateNewDefaultProject(out var main);
		Assert.NotNull(main.EntryNode);
		Assert.Single(main.ReturnNodes);
		Assert.Equal(main.EntryNode.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);

		var graphManager = main.Graph.Manager;
		var changes = new List<GraphChange>();
		using var subscription = main.Graph.Changes.Subscribe(changes.Add);

		// create a random method call used to test the connection
		var methodCall = new MethodCall(main.Graph);
		main.Graph.Manager.AddNode(methodCall);


		// This should also disconnect the entry node's existing exec connection
		graphManager.AddNewConnectionBetween(main.EntryNode.Outputs[0], methodCall.Inputs[0]);

		// main entry node was disconnected from the other node and is now connected to the method call
		Assert.Single(main.EntryNode.Outputs[0].Connections);
		Assert.Equal(main.EntryNode.Outputs[0].Connections[0], methodCall.Inputs[0]);

		// return node is not connected to anything
		Assert.Empty(main.ReturnNodes.Single().Inputs[0].Connections);

		Assert.Contains(changes, change => change is GraphChange.LinkAdded added && added.Source == main.EntryNode.Outputs[0] && added.Destination == methodCall.Inputs[0]);
		Assert.Contains(changes, change => change is GraphChange.LinkRemoved removed && removed.Source == main.EntryNode.Outputs[0] && removed.Destination == main.ReturnNodes.Single().Inputs[0]);
	}

	[Fact]
	public void ConnectTwoOutputsInOneInput_ShouldDisconnectFirstOutput()
	{
		var project = Project.CreateNewDefaultProject(out var main);
		Assert.NotNull(main.EntryNode);
		Assert.Single(main.ReturnNodes);
		Assert.Equal(main.EntryNode.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);

		var graphManager = main.Graph.Manager;
		var changes = new List<GraphChange>();
		using var subscription = main.Graph.Changes.Subscribe(changes.Add);

		var addNode1 = AddNewAddNodeToGraph<int>(main.Graph);
		var addNode2 = AddNewAddNodeToGraph<int>(main.Graph);
		var addNode3 = AddNewAddNodeToGraph<int>(main.Graph);

		// connect output of addNode1 to input of addNode3
		graphManager.AddNewConnectionBetween(addNode1.Outputs[0], addNode3.Inputs[0]);
		Assert.Single(addNode1.Outputs[0].Connections);
		Assert.Single(addNode3.Inputs[0].Connections);
		Assert.Equal(addNode1.Outputs[0].Connections[0], addNode3.Inputs[0]);
		changes.Clear();

		// connect output of addNode2 to input of addNode3. It should disconnect the existing connection
		graphManager.AddNewConnectionBetween(addNode2.Outputs[0], addNode3.Inputs[0]);
		Assert.Empty(addNode1.Outputs[0].Connections);
		Assert.Single(addNode2.Outputs[0].Connections);
		Assert.Single(addNode3.Inputs[0].Connections);
		Assert.Equal(addNode2.Outputs[0].Connections[0], addNode3.Inputs[0]);

		Assert.Contains(changes, change => change is GraphChange.LinkRemoved removed && removed.Source == addNode1.Outputs[0] && removed.Destination == addNode3.Inputs[0]);
	}

	[Fact]
	public void ConnectTwoExecOutputsInOneInput_ShouldAllow()
	{
		var project = Project.CreateNewDefaultProject(out var main);
		Assert.NotNull(main.EntryNode);
		Assert.Single(main.ReturnNodes);
		Assert.Equal(main.EntryNode.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);

		var graphManager = main.Graph.Manager;
		var changes = new List<GraphChange>();
		using var subscription = main.Graph.Changes.Subscribe(changes.Add);

		// create a random method call used to test the connection
		var methodCall = new MethodCall(main.Graph);
		main.Graph.Manager.AddNode(methodCall);

		// connect output of addNode1 to input of addNode3
		graphManager.AddNewConnectionBetween(methodCall.Outputs[0], main.ReturnNodes.Single().Inputs[0]);
		Assert.Single(main.EntryNode.Outputs[0].Connections);
		Assert.Equal(2, main.ReturnNodes.Single().Inputs[0].Connections.Count);
		Assert.Single(methodCall.Outputs[0].Connections);
		Assert.Equal(methodCall.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);
		Assert.DoesNotContain(changes, change => change is GraphChange.LinkRemoved);
	}

	[Fact]
	public void ConnectArrayToIEnumerableT_ShouldAllow()
	{
		var project = Project.CreateNewDefaultProject(out var main);
		Assert.NotNull(main.EntryNode);
		Assert.Single(main.ReturnNodes);
		Assert.Equal(main.EntryNode.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);

		var typeFactory = main.TypeFactory;

		var graphManager = main.Graph.Manager;

		// create a random method call used to test the connection
		var methodCall = AddMethodCall(main.Graph, typeFactory.Get<Array>(), nameof(Array.Empty));
		methodCall.Outputs[1].UpdateTypeAndTextboxVisibility(typeFactory.Get<string[]>(), overrideInitialType: true);

		var foreachNode = new ForeachNode(main.Graph);
		main.Graph.Manager.AddNode(foreachNode);

		// connect output of Array.Empty<string>() to input of foreachNode
		graphManager.AddNewConnectionBetween(methodCall.Outputs[1], foreachNode.Inputs[1]);
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

		var graphManager = main.Graph.Manager;

		// create a random method call used to test the connection
		var newListArray = new New(main.Graph);
		newListArray.Outputs[1].UpdateTypeAndTextboxVisibility(typeFactory.Get<List<string[]>>(), overrideInitialType: true);
		newListArray.GenericConnectionTypeDefined(newListArray.Outputs[1]);

		var foreachNode = new ForeachNode(main.Graph);
		main.Graph.Manager.AddNode(foreachNode);

		var foreachNode2 = new ForeachNode(main.Graph);
		main.Graph.Manager.AddNode(foreachNode2);

		// connect output of foreachNode into input of foreachNode2
		graphManager.AddNewConnectionBetween(foreachNode.Outputs[1], foreachNode2.Inputs[1]);

		// connect output of new List<string[]> to input of foreachNode
		graphManager.AddNewConnectionBetween(newListArray.Outputs[1], foreachNode.Inputs[1]);

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

		var graphManager = main.Graph.Manager;

		// output string[]
		var newArray = new New(main.Graph);
		newArray.Outputs[1].UpdateTypeAndTextboxVisibility(typeFactory.Get<string[]>(), overrideInitialType: true);
		newArray.GenericConnectionTypeDefined(newArray.Outputs[1]);

		var arrayGet = new ArrayGet(main.Graph);
		main.Graph.Manager.AddNode(arrayGet);

		// connect output of foreachNode into input of foreachNode2
		graphManager.AddNewConnectionBetween(newArray.Outputs[1], arrayGet.Inputs[0]);

		// Input of arrayGet should be string[], output should be string
		Assert.Equal(typeFactory.Get<string[]>(), arrayGet.Inputs[0].Type);
		Assert.Equal(typeFactory.Get<string>(), arrayGet.Outputs[0].Type);
	}
}
