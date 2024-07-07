using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;

namespace NodeDev.Tests;

public class NodeClassTypeCreatorTests
{
	[Theory]
	[MemberData(nameof(GraphExecutorTests.GetBuildOptions), MemberType = typeof(GraphExecutorTests))]
	public void SimpleProjectTest(SerializableBuildOptions options)
	{
		var project = new Project(Guid.NewGuid());

		var myClass = new NodeClass("TestClass", "MyProject", project);
		project.Classes.Add(myClass);

		myClass.Properties.Add(new(myClass, "MyProp", project.TypeFactory.Get<float>()));

		var creator = project.CreateNodeClassTypeCreator(options);

		creator.CreateProjectClassesAndAssembly();

		Assert.NotNull(creator.Assembly);
        Assert.Single(creator.Assembly.DefinedTypes.Where(x => x.IsVisible));
		Assert.Contains(creator.Assembly.DefinedTypes, x => x.Name == "TestClass");

		var instance = creator.Assembly.CreateInstance(myClass.Name);

		Assert.IsType(creator.GeneratedTypes[project.GetNodeClassType(myClass)].Type, instance);
	}

	[Fact]
	public void TestClassProjectOwnership()
	{
		var graph = GraphExecutorTests.CreateSimpleAddGraph<int, int>(out _, out _, out _);

		Assert.Equal(graph.SelfClass, graph.SelfClass.Project.Classes.First());
	}


    [Theory]
    [MemberData(nameof(GraphExecutorTests.GetBuildOptions), MemberType = typeof(GraphExecutorTests))]
    public void SimpleAddGenerationTest(SerializableBuildOptions options)
	{
		var graph = GraphExecutorTests.CreateSimpleAddGraph<int, int>(out _, out _, out _);

		var creator = graph.SelfClass.Project.CreateNodeClassTypeCreator(options);
		creator.CreateProjectClassesAndAssembly();
	}

    [Theory]
    [MemberData(nameof(GraphExecutorTests.GetBuildOptions), MemberType = typeof(GraphExecutorTests))]
    public void TestNewGetSet(SerializableBuildOptions options)
	{
		var project = new Project(Guid.NewGuid());

		var myClass = new NodeClass("Program", "MyProject", project);
		project.Classes.Add(myClass);

		var prop = new NodeClassProperty(myClass, "MyProp", project.TypeFactory.Get<int>());
		myClass.Properties.Add(prop);

		var graph = new Graph();
		var method = new NodeClassMethod(myClass, "Main", myClass.TypeFactory.Get<int>(), graph);
		method.IsStatic = true;
		myClass.Methods.Add(method);
		method.Parameters.Add(new("A", myClass.TypeFactory.Get<int>(), method));

		var entryNode = new EntryNode(graph);

		var returnNode = new ReturnNode(graph);
		returnNode.Inputs.Add(new("Result", entryNode, myClass.TypeFactory.Get<int>()));

		var newNode = new New(graph);
		newNode.Outputs[1].UpdateType(myClass.ClassTypeBase);

		var setProp = new SetPropertyOrField(graph);
		setProp.SetMemberTarget(prop);

		var getProp = new GetPropertyOrField(graph);
		getProp.SetMemberTarget(prop);

		graph.AddNode(entryNode, false);
		graph.AddNode(returnNode, false);
		graph.AddNode(newNode, false);
		graph.AddNode(getProp, false);
		graph.AddNode(setProp, false);

		// link the execution path
		graph.Connect(entryNode.Outputs[0], newNode.Inputs[0], false);
		graph.Connect(newNode.Outputs[0], setProp.Inputs[1], false); // set input 0 is the target, so use input 1 as the exec
		graph.Connect(setProp.Outputs[0], returnNode.Inputs[0], false);

		// link the rest
		graph.Connect(entryNode.Outputs[1], setProp.Inputs[2], false);
		graph.Connect(newNode.Outputs[1], setProp.Inputs[0], false);
		graph.Connect(newNode.Outputs[1], getProp.Inputs[0], false);
		graph.Connect(getProp.Outputs[0], returnNode.Inputs[1], false);

		var result = project.Run(options, [10]);

		Assert.Equal(10, result);
	}
}