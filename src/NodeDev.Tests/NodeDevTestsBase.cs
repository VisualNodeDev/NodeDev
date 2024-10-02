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
        graph.AddNode(addNode, false);

        addNode.Inputs[0].UpdateTypeAndTextboxVisibility(graph.Project.TypeFactory.Get<T>());
        addNode.Inputs[1].UpdateTypeAndTextboxVisibility(graph.Project.TypeFactory.Get<T>());
        addNode.Outputs[0].UpdateTypeAndTextboxVisibility(graph.Project.TypeFactory.Get<T>());

        return addNode;
    }

    protected MethodCall AddMethodCall(Graph graph, TypeBase type, string methodName, params TypeBase[] args)
    {
        var methods = type.GetMethods(methodName);

        var methodCall = new MethodCall(graph);
        graph.AddNode(methodCall, false);

        var method = methods.First(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(args));
        methodCall.SetMethodTarget(method);

        return methodCall;
    }
}
