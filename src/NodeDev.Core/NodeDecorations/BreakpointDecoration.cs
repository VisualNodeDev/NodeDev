using NodeDev.Core.Types;

namespace NodeDev.Core.NodeDecorations;

/// <summary>
/// Decoration to mark a node as having a breakpoint for debugging.
/// Only non-inlinable nodes (nodes with exec connections) can have breakpoints.
/// </summary>
public class BreakpointDecoration : INodeDecoration
{
	public static BreakpointDecoration Instance { get; } = new();

	private BreakpointDecoration() { }

	public string Serialize() => "breakpoint";

	public static new INodeDecoration Deserialize(TypeFactory typeFactory, string serialized)
	{
		return Instance;
	}
}
