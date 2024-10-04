using NodeDev.Core;
using NodeDev.Core.Connections;

namespace NodeDev.Blazor.Services.GraphManager;

public interface IGraphCanvas
{
	Graph Graph { get; }

	void UpdatePortColor(Connection connection);

	void RemoveLinkFromGraphCanvas(Connection source, Connection destination);
}
