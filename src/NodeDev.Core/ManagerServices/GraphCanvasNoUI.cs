using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;

namespace NodeDev.Core.ManagerServices;

/// <summary>
/// Represents a GraphCanvas that doesn't have a UI associated.
/// This is used when we need to update stuff in a graph but we don't have a UI to update.
/// </summary>
internal class GraphCanvasNoUI : IGraphCanvas
{
	public GraphCanvasNoUI(Graph graph)
	{
		Graph = graph;
	}

	public Graph Graph { get; }

	public void AddLinkToGraphCanvas(Connection source, Connection destination)
	{
	}

	public void AddNode(Node node)
	{
	}

	public void Refresh(Node node)
	{
	}

	public void RefreshAll()
	{
	}

	public void RemoveLinkFromGraphCanvas(Connection source, Connection destination)
	{
	}

	public void RemoveNode(Node node)
	{
	}

	public void UpdatePortColor(Connection connection)
	{
	}
}
