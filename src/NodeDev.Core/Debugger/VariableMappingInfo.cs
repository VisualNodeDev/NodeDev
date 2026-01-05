namespace NodeDev.Core.Debugger;

/// <summary>
/// Maps a connection to its corresponding local variable in the generated code.
/// Used for variable inspection during debugging.
/// </summary>
public class ConnectionVariableMapping
{
	/// <summary>
	/// The unique identifier of the connection (GraphIndex).
	/// </summary>
	public required int ConnectionGraphIndex { get; init; }

	/// <summary>
	/// The name of the local variable in the generated code.
	/// </summary>
	public required string VariableName { get; init; }

	/// <summary>
	/// The slot index of the local variable in the method's local variable table.
	/// Used by ICorDebug to retrieve the variable value.
	/// </summary>
	public required int SlotIndex { get; init; }

	/// <summary>
	/// The fully qualified name of the class containing this variable's method.
	/// </summary>
	public required string ClassName { get; init; }

	/// <summary>
	/// The name of the method containing this variable.
	/// </summary>
	public required string MethodName { get; init; }
}

/// <summary>
/// Collection of all variable mappings for a compiled project.
/// Allows lookup of variable information by connection graph index.
/// </summary>
public class VariableMappingInfo
{
	/// <summary>
	/// List of all connection-to-variable mappings in the compiled project.
	/// </summary>
	public List<ConnectionVariableMapping> Mappings { get; init; } = new();

	/// <summary>
	/// Gets the variable mapping for a specific connection graph index.
	/// </summary>
	/// <param name="connectionGraphIndex">The graph index of the connection.</param>
	/// <returns>The mapping if found, null otherwise.</returns>
	public ConnectionVariableMapping? GetMapping(int connectionGraphIndex)
	{
		return Mappings.FirstOrDefault(m => m.ConnectionGraphIndex == connectionGraphIndex);
	}

	/// <summary>
	/// Gets all mappings for a specific method.
	/// </summary>
	/// <param name="className">The class name.</param>
	/// <param name="methodName">The method name.</param>
	/// <returns>List of mappings for the specified method.</returns>
	public List<ConnectionVariableMapping> GetMappingsForMethod(string className, string methodName)
	{
		return Mappings.Where(m => m.ClassName == className && m.MethodName == methodName).ToList();
	}
}
