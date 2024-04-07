using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Nodes.Flow;

namespace NodeDev.Tests;

public class GraphExecutorTests
{
	public static Graph CreateSimpleAddGraph<TIn, TOut>(out Core.Nodes.Flow.EntryNode entryNode, out Core.Nodes.Flow.ReturnNode returnNode, out Core.Nodes.Math.Add addNode)
	{
		var project = new Project(Guid.NewGuid());
		var nodeClass = new NodeClass("Program", "Test", project);
		project.Classes.Add(nodeClass);

		var graph = new Graph();
		var method = new NodeClassMethod(nodeClass, "Main", nodeClass.TypeFactory.Get<TOut>(), graph);
		nodeClass.Methods.Add(method);
		graph.SelfMethod = nodeClass.Methods.First();

		method.Parameters.Add(new("A", nodeClass.TypeFactory.Get<TIn>(), method));
		method.Parameters.Add(new("B", nodeClass.TypeFactory.Get<TIn>(), method));

		entryNode = new Core.Nodes.Flow.EntryNode(graph);

		returnNode = new Core.Nodes.Flow.ReturnNode(graph);
		returnNode.Inputs.Add(new("Result", entryNode, nodeClass.TypeFactory.Get<TOut>()));

		addNode = new Core.Nodes.Math.Add(graph);

		addNode.Inputs[0].UpdateType(nodeClass.TypeFactory.Get<TIn>());
		addNode.Inputs[1].UpdateType(nodeClass.TypeFactory.Get<TIn>());
		addNode.Outputs[0].UpdateType(nodeClass.TypeFactory.Get<TOut>());

		graph.AddNode(entryNode);
		graph.AddNode(addNode);
		graph.AddNode(returnNode);

		graph.Connect(entryNode.Outputs[0], returnNode.Inputs[0]);

		graph.Connect(entryNode.Outputs[1], addNode.Inputs[0]);
		graph.Connect(entryNode.Outputs[2], addNode.Inputs[1]);
		graph.Connect(addNode.Outputs[0], returnNode.Inputs[1]);

		// preprocess every graph everywhere
		foreach (var node in project.Classes)
		{
			foreach (var m in nodeClass.Methods)
			{
				m.Graph.PreprocessGraph();
			}
		}

		return graph;
	}

	[Fact]
	public void SimpleAdd()
	{
		var graph = CreateSimpleAddGraph<int, int>(out _, out _, out _);

		var executor = new Core.GraphExecutor(graph, null);

		var outputs = new object?[2];
		executor.Execute(null, new object?[] { null, 1, 2 }, outputs);

		Assert.Equal(3, outputs[1]);
	}

	[Fact]
	public void SimpleAdd_CheckTypeFloat()
	{
		var graph = CreateSimpleAddGraph<float, float>(out _, out _, out _);

		var executor = new Core.GraphExecutor(graph, null);

		var outputs = new object?[2];
		executor.Execute(null, new object?[] { null, 1.5f, 2f }, outputs);

		Assert.Equal(3.5f, outputs[1]);
	}

	[Fact]
	public void TestBranch()
	{
		var graph = CreateSimpleAddGraph<int, int>(out var entryNode, out var returnNode1, out var addNode);
		graph.Disconnect(entryNode.Outputs[0], returnNode1.Inputs[0]);
		graph.Disconnect(returnNode1.Inputs[1], addNode.Outputs[0]);
		returnNode1.Inputs[1].UpdateTextboxText("1");

		var smallerThan = new Core.Nodes.Math.SmallerThan(graph);
		smallerThan.Inputs[0].UpdateType(graph.SelfClass.TypeFactory.Get<int>());
		smallerThan.Inputs[1].UpdateType(graph.SelfClass.TypeFactory.Get<int>());
		smallerThan.Inputs[1].UpdateTextboxText("0");
		graph.AddNode(smallerThan);
		graph.Connect(addNode.Outputs[0], smallerThan.Inputs[0]);

		var returnNode2 = new Core.Nodes.Flow.ReturnNode(graph);
		returnNode2.Inputs.Add(new("Result", entryNode, graph.SelfClass.TypeFactory.Get<int>()));
		returnNode2.Inputs[1].UpdateTextboxText("0");
		graph.AddNode(returnNode2);

		var branchNode = new Core.Nodes.Flow.Branch(graph);
		graph.Connect(entryNode.Outputs[0], branchNode.Inputs[0]);
		graph.Connect(smallerThan.Outputs[0], branchNode.Inputs[1]);
		graph.AddNode(branchNode);

		graph.Connect(branchNode.Outputs[0], returnNode1.Inputs[0]);
		graph.Connect(branchNode.Outputs[1], returnNode2.Inputs[0]);

		graph.PreprocessGraph();

		var executor = new Core.GraphExecutor(graph, null);

		var outputs = new object?[2];
		executor.Execute(null, new object?[] { null, 1.5f, 2f }, outputs);
		Assert.Equal(0, outputs[1]);

		executor.Execute(null, new object?[] { null, -1.5f, -2f }, outputs);
		Assert.Equal(1, outputs[1]);
	}

	[Fact]
	public void TestProjectRun()
	{
		var graph = CreateSimpleAddGraph<int, int>(out var entryNode, out var returnNode1, out var addNode);
		graph.Disconnect(entryNode.Outputs[0], returnNode1.Inputs[0]);
		graph.Disconnect(returnNode1.Inputs[1], addNode.Outputs[0]);
		returnNode1.Inputs[1].UpdateTextboxText("1");

		var smallerThan = new Core.Nodes.Math.SmallerThan(graph);
		graph.AddNode(smallerThan);
		smallerThan.Inputs[0].UpdateType(graph.SelfClass.TypeFactory.Get<int>());
		smallerThan.Inputs[1].UpdateType(graph.SelfClass.TypeFactory.Get<int>());
		smallerThan.Inputs[1].UpdateTextboxText("0");
		graph.Connect(addNode.Outputs[0], smallerThan.Inputs[0]);

		var returnNode2 = new Core.Nodes.Flow.ReturnNode(graph);
		graph.AddNode(returnNode2);
		returnNode2.Inputs.Add(new("Result", entryNode, graph.SelfClass.TypeFactory.Get<int>()));
		returnNode2.Inputs[1].UpdateTextboxText("0");

		var branchNode = new Core.Nodes.Flow.Branch(graph);
		graph.AddNode(branchNode);
		graph.Connect(entryNode.Outputs[0], branchNode.Inputs[0]);
		graph.Connect(smallerThan.Outputs[0], branchNode.Inputs[1]);

		graph.Connect(branchNode.Outputs[0], returnNode1.Inputs[0]);
		graph.Connect(branchNode.Outputs[1], returnNode2.Inputs[0]);

		var outputs = graph.SelfClass.Project.Run(new object?[] { null, 1.5f, 2f });

		Assert.Equal(0, outputs);

		outputs = graph.SelfClass.Project.Run(new object?[] { null, -1.5f, -2f });
		Assert.Equal(1, outputs);
	}
}