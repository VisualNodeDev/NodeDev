using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using System.Reactive.Linq;

namespace NodeDev.Tests;

public class EventsTests
{
	[Fact]
	public void TestPropertyRenameAndTypeChange()
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

		var entryNode = new Core.Nodes.Flow.EntryNode(graph);
		entryNode.Outputs.Add(new("A", entryNode, myClass.TypeFactory.Get<int>()));

		var returnNode = new Core.Nodes.Flow.ReturnNode(graph);
		returnNode.Inputs.Add(new("Result", entryNode, myClass.TypeFactory.Get<int>()));

		var newNode = new New(graph);
		newNode.Outputs[1].UpdateTypeAndTextboxVisibility(myClass.ClassTypeBase);

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

		bool raised = false;
		project.GraphChanged.Subscribe(x =>
		{
			Assert.Same(graph, x.Graph);

			raised = true;
		});

		prop.Rename("NewName");
		Assert.True(raised);

		raised = false;

		prop.ChangeType(project.TypeFactory.Get<float>());
		Assert.True(raised);
	}

}