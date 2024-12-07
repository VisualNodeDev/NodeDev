using NodeDev.Core;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Math;
using NodeDev.Core.Types;

namespace NodeDev.Tests;

public class NodeDevTestsBase
{
	protected Add AddNewAddNodeToGraph<T>(Graph graph)
	{
		var addNode = new Add(graph);
		graph.Manager.AddNode(addNode);

		addNode.Inputs[0].UpdateTypeAndTextboxVisibility(graph.Project.TypeFactory.Get<T>(), overrideInitialType: true);
		addNode.Inputs[1].UpdateTypeAndTextboxVisibility(graph.Project.TypeFactory.Get<T>(), overrideInitialType: true);
		addNode.Outputs[0].UpdateTypeAndTextboxVisibility(graph.Project.TypeFactory.Get<T>(), overrideInitialType: true);

		return addNode;
	}

	protected MethodCall AddMethodCall(Graph graph, TypeBase type, string methodName, params TypeBase[] args)
	{
		var methods = type.GetMethods(methodName);

		var methodCall = new MethodCall(graph);
		graph.Manager.AddNode(methodCall);

		var method = methods.First(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(args));
		methodCall.SetMethodTarget(method);

		return methodCall;
	}
}
