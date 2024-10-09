using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Nodes.Flow;
using System.Diagnostics;

namespace NodeDev.Tests;

public class GraphExecutorTests
{
	public static Graph CreateSimpleAddGraph<TIn, TOut>(out Core.Nodes.Flow.EntryNode entryNode, out Core.Nodes.Flow.ReturnNode returnNode, out Core.Nodes.Math.Add addNode, bool isStatic = true)
	{
		var project = new Project(Guid.NewGuid());
		var nodeClass = new NodeClass("Program", "Test", project);
		project.Classes.Add(nodeClass);

		var graph = new Graph();
		var method = new NodeClassMethod(nodeClass, "Main", nodeClass.TypeFactory.Get<TOut>(), graph);
		method.IsStatic = isStatic;
		nodeClass.Methods.Add(method);
		graph.SelfMethod = nodeClass.Methods.First();

		method.Parameters.Add(new("A", nodeClass.TypeFactory.Get<TIn>(), method));
		method.Parameters.Add(new("B", nodeClass.TypeFactory.Get<TIn>(), method));

		entryNode = new Core.Nodes.Flow.EntryNode(graph);

		returnNode = new Core.Nodes.Flow.ReturnNode(graph);
		returnNode.Inputs.Add(new("Result", entryNode, nodeClass.TypeFactory.Get<TOut>()));

		addNode = new Core.Nodes.Math.Add(graph);

		addNode.Inputs[0].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<TIn>(), overrideInitialType: true);
		addNode.Inputs[1].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<TIn>(), overrideInitialType: true);
		addNode.Outputs[0].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<TOut>(), overrideInitialType: true);

		graph.AddNode(entryNode, false);
		graph.AddNode(addNode, false);
		graph.AddNode(returnNode, false);

		graph.Connect(entryNode.Outputs[0], returnNode.Inputs[0], false);

		graph.Connect(entryNode.Outputs[1], addNode.Inputs[0], false);
		graph.Connect(entryNode.Outputs[2], addNode.Inputs[1], false);
		graph.Connect(addNode.Outputs[0], returnNode.Inputs[1], false);

		return graph;
	}

	public static TheoryData<SerializableBuildOptions> GetBuildOptions() => new([new(true), new(false)]);

    [Theory]
	[MemberData(nameof(GetBuildOptions))]
    public void SimpleAdd(SerializableBuildOptions options)
	{
		var graph = CreateSimpleAddGraph<int, int>(out _, out _, out _);

		var output = graph.Project.Run(options, [1, 2]);

		Assert.Equal(3, output);
	}

    [Theory]
    [MemberData(nameof(GetBuildOptions))]
    public void SimpleAdd_CheckTypeFloat(SerializableBuildOptions options)
	{
		var graph = CreateSimpleAddGraph<float, float>(out _, out _, out _);

		var output = graph.Project.Run(options, [1.5f, 2f]);

		Assert.Equal(3.5f, output);
	}

    [Theory]
    [MemberData(nameof(GetBuildOptions))]
    public void TestBranch(SerializableBuildOptions options)
	{
		var graph = CreateSimpleAddGraph<int, int>(out var entryNode, out var returnNode1, out var addNode);
		graph.Disconnect(entryNode.Outputs[0], returnNode1.Inputs[0], false);
		graph.Disconnect(returnNode1.Inputs[1], addNode.Outputs[0], false);
		returnNode1.Inputs[1].UpdateTextboxText("1");

		var smallerThan = new Core.Nodes.Math.SmallerThan(graph);
		smallerThan.Inputs[0].UpdateTypeAndTextboxVisibility(graph.SelfClass.TypeFactory.Get<int>(), overrideInitialType: true);
		smallerThan.Inputs[1].UpdateTypeAndTextboxVisibility(graph.SelfClass.TypeFactory.Get<int>(), overrideInitialType: true);
		smallerThan.Inputs[1].UpdateTextboxText("0");
		graph.AddNode(smallerThan, false);
		graph.Connect(addNode.Outputs[0], smallerThan.Inputs[0], false);

		var returnNode2 = new Core.Nodes.Flow.ReturnNode(graph);
		returnNode2.Inputs.Add(new("Result", entryNode, graph.SelfClass.TypeFactory.Get<int>()));
		returnNode2.Inputs[1].UpdateTextboxText("0");
		graph.AddNode(returnNode2, false);

		var branchNode = new Core.Nodes.Flow.Branch(graph);
		graph.Connect(entryNode.Outputs[0], branchNode.Inputs[0], false);
		graph.Connect(smallerThan.Outputs[0], branchNode.Inputs[1], false);
		graph.AddNode(branchNode, false);

		graph.Connect(branchNode.Outputs[0], returnNode1.Inputs[0], false);
		graph.Connect(branchNode.Outputs[1], returnNode2.Inputs[0], false);

		var output = graph.Project.Run(options, [1, 2]);
		Assert.Equal(0, output);

		output = graph.Project.Run(options, [1, -2]);
		Assert.Equal(1, output);
	}

    [Theory]
    [MemberData(nameof(GetBuildOptions))]
    public void TestProjectRun(SerializableBuildOptions options)
	{
		var graph = CreateSimpleAddGraph<int, int>(out var entryNode, out var returnNode1, out var addNode);
		graph.Disconnect(entryNode.Outputs[0], returnNode1.Inputs[0], false);
		graph.Disconnect(returnNode1.Inputs[1], addNode.Outputs[0], false);
		returnNode1.Inputs[1].UpdateTextboxText("1");

		var smallerThan = new Core.Nodes.Math.SmallerThan(graph);
		graph.AddNode(smallerThan, false);
		smallerThan.Inputs[0].UpdateTypeAndTextboxVisibility(graph.SelfClass.TypeFactory.Get<int>(), overrideInitialType: true);
		smallerThan.Inputs[1].UpdateTypeAndTextboxVisibility(graph.SelfClass.TypeFactory.Get<int>(), overrideInitialType: true);
		smallerThan.Inputs[1].UpdateTextboxText("0");
		graph.Connect(addNode.Outputs[0], smallerThan.Inputs[0], false);

		var returnNode2 = new Core.Nodes.Flow.ReturnNode(graph);
		graph.AddNode(returnNode2, false);
		returnNode2.Inputs.Add(new("Result", entryNode, graph.SelfClass.TypeFactory.Get<int>()));
		returnNode2.Inputs[1].UpdateTextboxText("0");

		var branchNode = new Core.Nodes.Flow.Branch(graph);
		graph.AddNode(branchNode, false);
		graph.Connect(entryNode.Outputs[0], branchNode.Inputs[0], false);
		graph.Connect(smallerThan.Outputs[0], branchNode.Inputs[1], false);

		graph.Connect(branchNode.Outputs[0], returnNode1.Inputs[0], false);
		graph.Connect(branchNode.Outputs[1], returnNode2.Inputs[0], false);

		var output = graph.SelfClass.Project.Run(options, [1, 2]);

		Assert.Equal(0, output);

		output = graph.SelfClass.Project.Run(options, [-1, -2]);
		Assert.Equal(1, output);
	}
}