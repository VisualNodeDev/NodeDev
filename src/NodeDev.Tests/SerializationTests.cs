using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Nodes;

namespace NodeDev.Tests;

public class SerializationTests
{
    [Theory]
    [MemberData(nameof(GraphExecutorTests.GetBuildOptions), MemberType = typeof(GraphExecutorTests))]
    public void TestBasicSerialization(SerializableBuildOptions options)
	{
		var graph = GraphExecutorTests.CreateSimpleAddGraph<int, int>(out _, out _, out _);
		var project = graph.SelfClass.Project;

		var serialized = project.Serialize();
		var deserializedProject = Project.Deserialize(serialized);

		Assert.Single(deserializedProject.Classes);
		Assert.Single(deserializedProject.Classes.First().Methods);

		graph = deserializedProject.Classes.First().Methods.First().Graph; // swap the original graph with the deserialized one

		var output = graph.Project.Run(options, [1, 2]);

		Assert.Equal(3, output);
	}

    [Theory]
    [MemberData(nameof(GraphExecutorTests.GetBuildOptions), MemberType = typeof(GraphExecutorTests))]
    public void TestSerializationMethodCall(SerializableBuildOptions options)
	{
		var simpleGraph = GraphExecutorTests.CreateSimpleAddGraph<int, int>(out _, out _, out _, isStatic: false);
		var project = simpleGraph.SelfClass.Project;

		var testClass = new NodeClass("MyClass2", "Test", project);
		project.Classes.Add(testClass);

		var testMethodGraph = new Graph();
		var testMethod = new NodeClassMethod(testClass, "TestMethod", project.TypeFactory.Get<int>(), testMethodGraph);
		testMethodGraph.SelfMethod = testMethod;

		var methodCall = new MethodCall(testMethodGraph);
		methodCall.SetMethodTarget(project.Classes.First().Methods.First());

		var entryNode = new Core.Nodes.Flow.EntryNode(testMethodGraph);
		entryNode.Outputs.Add(new("A", entryNode, project.TypeFactory.Get<int>()));
		entryNode.Outputs.Add(new("B", entryNode, project.TypeFactory.Get<int>()));

		var returnNode = new Core.Nodes.Flow.ReturnNode(testMethodGraph);
		returnNode.Inputs.Add(new("Result", entryNode, project.TypeFactory.Get<int>()));


		testMethodGraph.AddNode(entryNode);
		testMethodGraph.AddNode(methodCall);
		testMethodGraph.AddNode(returnNode);

		testMethodGraph.Connect(entryNode.Outputs[0], methodCall.Inputs[0], false);// exec from entry to method call
		testMethodGraph.Connect(entryNode.Outputs[1], methodCall.Inputs[2], false); // A to A
		testMethodGraph.Connect(entryNode.Outputs[2], methodCall.Inputs[3], false); // B to B
		testMethodGraph.Connect(methodCall.Outputs[0], returnNode.Inputs[0], false); // exec from method to return node
		testMethodGraph.Connect(methodCall.Outputs[1], returnNode.Inputs[1], false); // method call result to return node result

		var output = testMethodGraph.Project.Run(options, [1, 2]);

		Assert.Equal(3, output);
	}

}