using NodeDev.Core;
using NodeDev.Core.Nodes.Math;

namespace NodeDev.Tests;

public class NodeProviderTests
{

	[Fact]
	public void TestsNodeMethod()
	{
		var graph = GraphExecutorTests.CreateSimpleAddGraph<int, int>(out _, out _, out _);
		var project = new Project(Guid.NewGuid());

		project.AddClass(graph.SelfClass);

		var methods = NodeProvider.Search(project, graph.SelfMethod.Name, null);

		Assert.Contains(methods, x => x is NodeProvider.MethodCallNode methodCall && methodCall.MethodInfo == graph.SelfMethod);

		methods = NodeProvider.Search(project, graph.SelfMethod.Name + "asd", null);
		Assert.DoesNotContain(methods, x => x is NodeProvider.MethodCallNode methodCall && methodCall.MethodInfo == graph.SelfMethod);
	}

	[Fact]
	public void ScopeAwareSearchReturnsNodesInRootScope()
	{
		var graph = GraphExecutorTests.CreateSimpleAddGraph<int, int>(out _, out _, out _);

		var results = NodeProvider.Search(graph, "Add", null, null).ToList();

		Assert.Contains(results, result => result.Type == typeof(Add));
	}

}
