using NodeDev.Core;
using NodeDev.Core.Class;
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

		Assert.IsType(creator.GeneratedTypes[project.GetNodeClassType(myClass)], instance);
	}

}