using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Nodes;

namespace NodeDev.Tests;

public class ProjectMutationTests
{
	[Fact]
	public void RemoveMethodRemovesItFromTheSerializedDomainModel()
	{
		var project = Project.CreateNewDefaultProject();
		var nodeClass = project.Classes.Single();
		var method = new NodeClassMethod(nodeClass, "Temporary", project.TypeFactory.Void);
		nodeClass.AddMethod(method, createEntryAndReturn: true);

		nodeClass.RemoveMethod(method);

		Assert.DoesNotContain(method, nodeClass.Methods);
		Assert.DoesNotContain("Temporary", project.Serialize());
	}

	[Fact]
	public void RemoveMethodRejectsDanglingMethodCalls()
	{
		var project = Project.CreateNewDefaultProject(out var main);
		var nodeClass = project.Classes.Single();
		var method = new NodeClassMethod(nodeClass, "UsedMethod", project.TypeFactory.Void);
		nodeClass.AddMethod(method, createEntryAndReturn: true);
		var call = new MethodCall(main.Graph);
		call.SetMethodTarget(method);
		main.Manager.AddNode(call);

		Assert.Throws<InvalidOperationException>(() => nodeClass.RemoveMethod(method));
		Assert.Contains(method, nodeClass.Methods);
	}

	[Fact]
	public void RemoveClassRejectsReferencedClassTypes()
	{
		var project = Project.CreateNewDefaultProject();
		var referencedClass = new NodeClass("Referenced", "Tests", project);
		project.AddClass(referencedClass);
		project.Classes[0].Properties.Add(new NodeClassProperty(project.Classes[0], "Reference", referencedClass.ClassTypeBase));

		Assert.Throws<InvalidOperationException>(() => project.RemoveClass(referencedClass));
		Assert.Contains(referencedClass, project.Classes);
	}

	[Fact]
	public void RemoveClassRejectsStaticMethodCallsWithoutClassTypedConnections()
	{
		var project = Project.CreateNewDefaultProject(out var main);
		var referencedClass = new NodeClass("Referenced", "Tests", project);
		project.AddClass(referencedClass);
		var referencedMethod = new NodeClassMethod(referencedClass, "StaticMethod", project.TypeFactory.Void, isStatic: true);
		referencedClass.AddMethod(referencedMethod, createEntryAndReturn: true);
		var call = new MethodCall(main.Graph);
		call.SetMethodTarget(referencedMethod);
		main.Manager.AddNode(call);

		Assert.Throws<InvalidOperationException>(() => project.RemoveClass(referencedClass));
		Assert.Contains(referencedClass, project.Classes);
	}
}
