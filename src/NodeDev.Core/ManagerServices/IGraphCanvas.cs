using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;

namespace NodeDev.Core.ManagerServices;

public interface IGraphCanvas
{
	Graph Graph { get; }

	void UpdatePortColor(Connection connection);

	void RemoveLinkFromGraphCanvas(Connection source, Connection destination);

	void AddLinkToGraphCanvas(Connection source, Connection destination);

	void RemoveNode(Node node);

	void AddNode(Node node);

	void Refresh(Node node);

	void RefreshAll() => Graph.RaiseGraphChanged(true);
}
