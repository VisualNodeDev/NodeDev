using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.Connections;
using NodeDev.Core.Debugger;
using NodeDev.Core.Nodes;

namespace NodeDev.Core.CodeGeneration;

/// <summary>
/// Context for generating Roslyn syntax from a node graph.
/// Manages symbol tables, variable names, and auxiliary statements.
/// </summary>
public class GenerationContext
{
	private readonly Dictionary<string, string> _connectionToVariableName = new();
	private readonly List<StatementSyntax> _auxiliaryStatements = new();
	private readonly HashSet<string> _usedVariableNames = new();
	private int _uniqueCounter = 0;

	// Track variable mappings for debugging
	private readonly List<ConnectionVariableMapping> _variableMappings = new();
	private string? _currentClassName;
	private string? _currentMethodName;

	public GenerationContext(bool isDebug)
	{
		IsDebug = isDebug;
	}

	/// <summary>
	/// Whether to generate debug-friendly code (e.g., with event calls for stepping)
	/// </summary>
	public bool IsDebug { get; }

	/// <summary>
	/// Collection of nodes with breakpoints and their line number mappings.
	/// This is populated during code generation to track where breakpoints should be set.
	/// </summary>
	public List<NodeBreakpointInfo> BreakpointMappings { get; } = new();

	/// <summary>
	/// Collection of connection-to-variable mappings for debugging.
	/// </summary>
	public List<ConnectionVariableMapping> VariableMappings => _variableMappings;

	/// <summary>
	/// Sets the current class and method being generated.
	/// Used for variable mapping tracking.
	/// </summary>
	public void SetCurrentMethod(string className, string methodName)
	{
		_currentClassName = className;
		_currentMethodName = methodName;
	}

	/// <summary>
	/// Gets the variable name for a connection, or null if not yet registered
	/// </summary>
	public string? GetVariableName(Connection connection)
	{
		_connectionToVariableName.TryGetValue(connection.Id, out var name);
		return name;
	}

	/// <summary>
	/// Registers a variable name for a connection
	/// </summary>
	public void RegisterVariableName(Connection connection, string variableName)
	{
		_connectionToVariableName[connection.Id] = variableName;

		// Track this mapping for debugging (if we have method context)
		if (_currentClassName != null && _currentMethodName != null && connection.GraphIndex >= 0)
		{
			// Note: SlotIndex will be -1 initially, as we don't know it until after compilation
			// It could be determined later by analyzing the PDB, but for now we'll use variable name lookup
			_variableMappings.Add(new ConnectionVariableMapping
			{
				ConnectionGraphIndex = connection.GraphIndex,
				VariableName = variableName,
				SlotIndex = -1, // Unknown at code generation time
				ClassName = _currentClassName,
				MethodName = _currentMethodName
			});
		}
	}

	/// <summary>
	/// Generates a unique variable name based on a hint
	/// </summary>
	public string GetUniqueName(string hint)
	{
		// Sanitize the hint to make it a valid C# identifier
		var sanitized = SanitizeIdentifier(hint);

		// If the name is already unique, return it
		if (_usedVariableNames.Add(sanitized))
			return sanitized;

		// Otherwise, append a counter until we find a unique name
		string uniqueName;
		do
		{
			uniqueName = $"{sanitized}_{_uniqueCounter++}";
		} while (!_usedVariableNames.Add(uniqueName));

		return uniqueName;
	}

	/// <summary>
	/// Adds an auxiliary statement that needs to be emitted before the current operation
	/// </summary>
	public void AddAuxiliaryStatement(StatementSyntax statement)
	{
		_auxiliaryStatements.Add(statement);
	}

	/// <summary>
	/// Gets all auxiliary statements and clears the buffer
	/// </summary>
	public List<StatementSyntax> GetAndClearAuxiliaryStatements()
	{
		var statements = new List<StatementSyntax>(_auxiliaryStatements);
		_auxiliaryStatements.Clear();
		return statements;
	}

	/// <summary>
	/// Gets all auxiliary statements without clearing
	/// </summary>
	public IReadOnlyList<StatementSyntax> GetAuxiliaryStatements() => _auxiliaryStatements.AsReadOnly();

	private static string SanitizeIdentifier(string hint)
	{
		if (string.IsNullOrEmpty(hint))
			return "var";

		// Remove invalid characters
		var chars = hint.ToCharArray();
		for (int i = 0; i < chars.Length; i++)
		{
			if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
				chars[i] = '_';
		}

		var result = new string(chars);

		// Ensure it starts with a letter or underscore
		if (!char.IsLetter(result[0]) && result[0] != '_')
			result = "_" + result;

		// Avoid C# keywords
		if (SyntaxFacts.GetKeywordKind(result) != SyntaxKind.None ||
			SyntaxFacts.GetContextualKeywordKind(result) != SyntaxKind.None)
		{
			result = "@" + result;
		}

		return result;
	}
}
