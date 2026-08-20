using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;

namespace NodeDev.Core;

/// <summary>
/// Describes a domain graph change that projections can apply incrementally.
/// </summary>
public abstract record GraphChange
{
	public sealed record NodeAdded(Node Node) : GraphChange;

	public sealed record NodeRemoved(Node Node) : GraphChange;

	public sealed record NodeChanged(Node Node) : GraphChange;

	public sealed record LinkAdded(Connection Source, Connection Destination) : GraphChange;

	public sealed record LinkRemoved(Connection Source, Connection Destination) : GraphChange;

	public sealed record ConnectionChanged(Connection Connection) : GraphChange;

	public sealed record ProjectionReset : GraphChange;
}
