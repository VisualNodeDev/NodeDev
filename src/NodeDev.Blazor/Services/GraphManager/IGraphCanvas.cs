using NodeDev.Core;
using NodeDev.Core.Connections;

namespace NodeDev.Blazor.Services.GraphManager;

public interface IGraphCanvas
{
	Graph Graph { get; }

	void UpdatePortTypeAndColor(Connection connection);

	void RemoveLinkFromGraphCanvas(Connection source, Connection destination);
}
