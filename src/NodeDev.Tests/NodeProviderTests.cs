using NodeDev.Core;

namespace NodeDev.Tests;

public class NodeProviderTests
{

	[Fact]
	public void TestsNodeMethod()
	{
		var graph = GraphExecutorTests.CreateSimpleAddGraph<int, int>(out _, out _, out _);
		var project = new Project(Guid.NewGuid());

		project.Classes.Add(graph.SelfClass);

		var methods = NodeProvider.Search(project, graph.SelfMethod.Name, null);

		Assert.Contains(methods, x => x is NodeProvider.MethodCallNode methodCall && methodCall.MethodInfo == graph.SelfMethod);

		methods = NodeProvider.Search(project, graph.SelfMethod.Name + "asd", null);
		Assert.DoesNotContain(methods, x => x is NodeProvider.MethodCallNode methodCall && methodCall.MethodInfo == graph.SelfMethod);
	}

}