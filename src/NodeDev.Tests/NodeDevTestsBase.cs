using NodeDev.Core;
using NodeDev.Core.Nodes.Math;

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
}
