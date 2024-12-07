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
		project.AddClass(myClass);

		var prop = new NodeClassProperty(myClass, "MyProp", project.TypeFactory.Get<int>());
		myClass.Properties.Add(prop);

		var graph = new Graph();
		var method = new NodeClassMethod(myClass, "Main", myClass.TypeFactory.Get<int>(), graph);
		method.Parameters.Add(new("A", myClass.TypeFactory.Get<int>(), method));
		myClass.AddMethod(method, true);

		var entryNode = graph.Nodes.Values.OfType<EntryNode>().First();

		var returnNode = graph.Nodes.Values.OfType<ReturnNode>().First();

		var newNode = new New(graph);
		newNode.Outputs[1].UpdateTypeAndTextboxVisibility(myClass.ClassTypeBase, overrideInitialType: true);

		var setProp = new SetPropertyOrField(graph);
		setProp.SetMemberTarget(prop);

		var getProp = new GetPropertyOrField(graph);
		getProp.SetMemberTarget(prop);

		graph.Manager.AddNode(newNode);
		graph.Manager.AddNode(getProp);
		graph.Manager.AddNode(setProp);

		// link the execution path
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[0], newNode.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(newNode.Outputs[0], setProp.Inputs[1]); // set input 0 is the target, so use input 1 as the exec
		graph.Manager.AddNewConnectionBetween(setProp.Outputs[0], returnNode.Inputs[0]);

		// link the rest
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[1], setProp.Inputs[2]);
		graph.Manager.AddNewConnectionBetween(newNode.Outputs[1], setProp.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(newNode.Outputs[1], getProp.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(getProp.Outputs[0], returnNode.Inputs[1]);

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