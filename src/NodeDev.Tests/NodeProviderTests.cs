using NodeDev.Core;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Math;

namespace NodeDev.Tests;

public class NodeProviderTests
{

	[Fact]
	public void TestsNodeMethod()
	{
		var graph = GraphExecutorTests.CreateSimpleAddGraph<int, int>(out _, out _, out _);
		var project = graph.SelfClass.Project;

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

	[Fact]
	public void SearchFromIEnumerableIntReturnsClosedWhereExtensionMethod()
	{
		var project = Project.CreateNewDefaultProject(out var main);
		var sourceNode = new New(main.Graph);
		var enumerableOfInt = project.TypeFactory.Get<IEnumerable<int>>();
		sourceNode.Outputs[1].UpdateTypeAndTextboxVisibility(enumerableOfInt, overrideInitialType: true);
		main.Graph.Manager.AddNode(sourceNode);

		var predicateType = project.TypeFactory.Get<Func<int, bool>>();
		Assert.DoesNotContain(
			NodeProvider.Search(main.Graph, "MethodThatDoesNotExist", sourceNode.Outputs[1], null),
			result => result is NodeProvider.MethodCallNode);
		var whereResults =
			NodeProvider.Search(main.Graph, "Where", sourceNode.Outputs[1], null)
				.OfType<NodeProvider.MethodCallNode>()
				.ToList();
		var whereResult = Assert.Single(
			whereResults,
			result =>
					result.MethodInfo.DeclaringType.FullName == typeof(Enumerable).FullName &&
					result.MethodInfo.GetParameters().ElementAt(1).ParameterType == predicateType);

		Assert.Equal(enumerableOfInt, whereResult.MethodInfo.GetParameters().First().ParameterType);
		Assert.Equal(enumerableOfInt, whereResult.MethodInfo.ReturnType);
		Assert.False(whereResult.MethodInfo.CreateMethodInfo().ContainsGenericParameters);

		var whereNode = Assert.IsType<MethodCall>(main.Graph.Manager.AddNode(whereResult, _ => { }));
		Assert.Equal(enumerableOfInt, whereNode.Inputs[1].Type);
		Assert.Equal(predicateType, whereNode.Inputs[2].Type);
		Assert.Equal(enumerableOfInt, whereNode.Outputs[1].Type);
		Assert.Equal(2, whereNode.AlternatesOverloads.Count());
		main.Graph.Manager.AddNewConnectionBetween(sourceNode.Outputs[1], whereNode.Inputs[1]);
		Assert.Contains(sourceNode.Outputs[1], whereNode.Inputs[1].Connections);

		var restoredProject = Project.Deserialize(project.Serialize());
		var restoredWhereNode = Assert.Single(restoredProject.GetNodes<MethodCall>(), node => node.TargetMethod?.Name == "Where");
		Assert.Equal(restoredProject.TypeFactory.Get<IEnumerable<int>>(), restoredWhereNode.Inputs[1].Type);
		Assert.Equal(restoredProject.TypeFactory.Get<Func<int, bool>>(), restoredWhereNode.Inputs[2].Type);
		Assert.Single(restoredWhereNode.Inputs[1].Connections);
		Assert.False(restoredWhereNode.TargetMethod!.CreateMethodInfo().ContainsGenericParameters);
	}

}
