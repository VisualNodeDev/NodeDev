namespace NodeDev.Core.Debugger;

/// <summary>
/// Information about a node with a breakpoint set.
/// Maps a node to its location in the generated source code.
/// </summary>
public class NodeBreakpointInfo
{
	/// <summary>
	/// The unique ID of the node that has a breakpoint.
	/// </summary>
	public required string NodeId { get; init; }

	/// <summary>
	/// The name of the node for display purposes.
	/// </summary>
	public required string NodeName { get; init; }

	/// <summary>
	/// The fully qualified name of the class containing this node's method.
	/// </summary>
	public required string ClassName { get; init; }

	/// <summary>
	/// The name of the method containing this node.
	/// </summary>
	public required string MethodName { get; init; }

	/// <summary>
	/// The line number in the generated source code where this node's statement begins.
	/// For #line directive approach, this is a virtual line number (e.g., 10000, 11000, 12000).
	/// 1-based line number.
	/// </summary>
	public required int LineNumber { get; init; }
	
	/// <summary>
	/// The source file name used in #line directives.
	/// For virtual files, this is a generated name like "NodeDev_ClassName_MethodName.g.cs".
	/// </summary>
	public string SourceFile { get; init; } = string.Empty;
}

/// <summary>
/// Collection of all breakpoint information for a compiled project.
/// </summary>
public class BreakpointMappingInfo
{
	/// <summary>
	/// List of all nodes with breakpoints in the compiled project.
	/// </summary>
	public List<NodeBreakpointInfo> Breakpoints { get; init; } = new();

	/// <summary>
	/// The path to the generated source file (for reference).
	/// </summary>
	public string? SourceFilePath { get; init; }
}
