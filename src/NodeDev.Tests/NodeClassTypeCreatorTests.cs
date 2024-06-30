using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;

namespace NodeDev.Tests;

public class NodeClassTypeCreatorTests
{
	[Fact]
	public void SimpleProjectTest()
	{
		var project = new Project(Guid.NewGuid());

		var myClass = new NodeClass("TestClass", "MyProject", project);
		project.Classes.Add(myClass);

		myClass.Properties.Add(new(myClass, "MyProp", project.TypeFactory.Get<float>()));

		var creator = new NodeClassTypeCreator();

		var assembly = creator.CreateProjectClassesAndAssembly(project);

		Assert.Single(assembly.DefinedTypes);
		Assert.Contains(assembly.DefinedTypes, x => x.Name == "TestClass");

		var instance = assembly.CreateInstance(myClass.Name);

		Assert.IsType(creator.GeneratedTypes[project.GetNodeClassType(myClass)].Type, instance);
	}

	[Fact]
	public void TestClassProjectOwnership()
	{
		var graph = GraphExecutorTests.CreateSimpleAddGraph<int, int>(out _, out _, out _);

		Assert.Equal(graph.SelfClass, graph.SelfClass.Project.Classes.First());
	}


	[Fact]
	public void SimpleAddGenerationTest()
	{
		var graph = GraphExecutorTests.CreateSimpleAddGraph<int, int>(out _, out _, out _);

		graph.SelfClass.Project.NodeClassTypeCreator = new();
		var assembly = graph.SelfClass.Project.NodeClassTypeCreator.CreateProjectClassesAndAssembly(graph.SelfClass.Project);

	}

	[Fact]
	public void TestNewGetSet()
	{
		var project = new Project(Guid.NewGuid());

		var myClass = new NodeClass("Program", "MyProject", project);
		project.Classes.Add(myClass);

		var prop = new NodeClassProperty(myClass, "MyProp", project.TypeFactory.Get<int>());
		myClass.Properties.Add(prop);

		var graph = new Graph();
		var method = new NodeClassMethod(myClass, "Main", myClass.TypeFactory.Get<int>(), graph);
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

		graph.AddNode(entryNode);
		graph.AddNode(returnNode);
		graph.AddNode(newNode);
		graph.AddNode(getProp);
		graph.AddNode(setProp);

		// link the execution path
		graph.Connect(entryNode.Outputs[0], newNode.Inputs[0]);
		graph.Connect(newNode.Outputs[0], setProp.Inputs[1]); // set input 0 is the target, so use input 1 as the exec
		graph.Connect(setProp.Outputs[0], returnNode.Inputs[0]);

		// link the rest
		graph.Connect(entryNode.Outputs[1], setProp.Inputs[2]);
		graph.Connect(newNode.Outputs[1], setProp.Inputs[0]);
		graph.Connect(newNode.Outputs[1], getProp.Inputs[0]);
		graph.Connect(getProp.Outputs[0], returnNode.Inputs[1]);

		var result = project.Run(new object?[] { null, 10 });

		Assert.Equal(10, result);
	}
}