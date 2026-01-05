using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.Connections;
using NodeDev.Core.Debugger;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.CodeGeneration;

/// <summary>
/// Generates Roslyn syntax trees from node graphs
/// </summary>
public class RoslynGraphBuilder
{
	private readonly Graph _graph;
	private readonly GenerationContext _context;

	public RoslynGraphBuilder(Graph graph, bool isDebug)
	{
		_graph = graph;
		_context = new GenerationContext(isDebug);
	}

	/// <summary>
	/// Constructor that accepts an existing context (for sub-builders)
	/// </summary>
	public RoslynGraphBuilder(Graph graph, GenerationContext context)
	{
		_graph = graph;
		_context = context;
	}
	
	/// <summary>
	/// Gets the breakpoint mappings collected during code generation.
	/// </summary>
	public List<NodeBreakpointInfo> GetBreakpointMappings() => _context.BreakpointMappings;

	/// <summary>
	/// Builds a complete method syntax from the graph
	/// </summary>
	public MethodDeclarationSyntax BuildMethod()
	{
		var method = _graph.SelfMethod;

		// Find the entry node
		var entryNode = _graph.Nodes.Values.FirstOrDefault(x => x is EntryNode)
			?? throw new Exception($"No entry node found in graph {method.Name}");

		var entryOutput = entryNode.Outputs.FirstOrDefault()
			?? throw new Exception("Entry node has no output");

		// Register method parameters in context (from Entry node)
		// Skip the first output (Exec), the rest are parameters
		for (int i = 1; i < entryNode.Outputs.Count; i++)
		{
			var output = entryNode.Outputs[i];
			// Register with the parameter name directly
			_context.RegisterVariableName(output, output.Name);
		}

		// Pre-declare variables for node outputs (similar to old CreateOutputsLocalVariableExpressions)
		var variableDeclarations = new List<LocalDeclarationStatementSyntax>();
		foreach (var node in _graph.Nodes.Values)
		{
			if (node.CanBeInlined)
				continue; // inline nodes don't need pre-declared variables

			// Entry node parameters are not pre-declared, they are method parameters
			if (node is EntryNode)
				continue;

			foreach (var output in node.Outputs)
			{
				if (output.Type.IsExec)
					continue;

				var varName = _context.GetUniqueName($"{node.Name}_{output.Name}");
				_context.RegisterVariableName(output, varName);

				// Declare: var <varName> = default(Type);
				var typeSyntax = RoslynHelpers.GetTypeSyntax(output.Type);
				var declarator = SF.VariableDeclarator(SF.Identifier(varName))
					.WithInitializer(SF.EqualsValueClause(
						SF.DefaultExpression(typeSyntax)));

				variableDeclarations.Add(
					SF.LocalDeclarationStatement(
						SF.VariableDeclaration(SF.IdentifierName("var"))
							.WithVariables(SF.SingletonSeparatedList(declarator))));
			}
		}

		// Build the execution flow starting from entry
		var chunks = _graph.GetChunks(entryOutput, allowDeadEnd: false);
		
		// Get full class name for breakpoint info
		string fullClassName = $"{_graph.SelfClass.Namespace}.{_graph.SelfClass.Name}";
		
		// In debug builds, always track line numbers for all nodes (not just those with breakpoints)
		// This allows breakpoints to be set dynamically during debugging
		var bodyStatements = _context.IsDebug
			? BuildStatementsWithBreakpointTracking(chunks, fullClassName, method.Name)
			: BuildStatements(chunks);

		// Combine variable declarations with body statements
		var allStatements = variableDeclarations.Cast<StatementSyntax>()
			.Concat(bodyStatements)
			.ToList();

		// Add return statement if needed
		if (!method.HasReturnValue)
		{
			allStatements.Add(SF.ReturnStatement());
		}

		// Create the method declaration
		var modifiers = new List<SyntaxToken>();
		modifiers.Add(SF.Token(SyntaxKind.PublicKeyword));
		if (method.IsStatic)
			modifiers.Add(SF.Token(SyntaxKind.StaticKeyword));

		var returnType = method.HasReturnValue
			? RoslynHelpers.GetTypeSyntax(method.ReturnType)
			: SF.PredefinedType(SF.Token(SyntaxKind.VoidKeyword));

		var parameters = method.Parameters
			.Where(p => !p.ParameterType.IsExec)
			.Select(p => SF.Parameter(SF.Identifier(p.Name))
				.WithType(RoslynHelpers.GetTypeSyntax(p.ParameterType)));

		var methodDeclaration = SF.MethodDeclaration(returnType, SF.Identifier(method.Name))
			.WithModifiers(SF.TokenList(modifiers))
			.WithParameterList(SF.ParameterList(SF.SeparatedList(parameters)))
			.WithBody(SF.Block(allStatements));

		return methodDeclaration;
	}

	/// <summary>
	/// Builds statements from node path chunks
	/// </summary>
	internal List<StatementSyntax> BuildStatements(Graph.NodePathChunks chunks)
	{
		var statements = new List<StatementSyntax>();

		foreach (var chunk in chunks.Chunks)
		{
			var node = chunk.Input.Parent;
			
			// Resolve inputs first
			foreach (var input in node.Inputs)
			{
				ResolveInputConnection(input);
			}

			// Get auxiliary statements generated during input resolution (like inline variable declarations)
			// These need to be added BEFORE the main statement
			statements.AddRange(_context.GetAndClearAuxiliaryStatements());

			try
			{
				// Generate the statement for this node
				var statement = node.GenerateRoslynStatement(chunk.SubChunk, _context);

				// Add the main statement
				statements.Add(statement);
			}
			catch (Exception ex) when (ex is not BuildError)
			{
				throw new BuildError($"Failed to generate statement for node type {node.GetType().Name}: {ex.Message}", node, ex);
			}
		}

		return statements;
	}
	
	/// <summary>
	/// Builds statements from node path chunks, tracking line numbers for breakpoints.
	/// Returns the statements and populates breakpoint info in the context.
	/// </summary>
	internal List<StatementSyntax> BuildStatementsWithBreakpointTracking(Graph.NodePathChunks chunks, string className, string methodName)
	{
		var statements = new List<StatementSyntax>();
		string virtualFileName = $"NodeDev_{className}_{methodName}.g.cs";
		int nodeExecutionOrder = 0; // Track execution order of ALL nodes

		foreach (var chunk in chunks.Chunks)
		{
			var node = chunk.Input.Parent;
			
			// Resolve inputs first
			foreach (var input in node.Inputs)
			{
				ResolveInputConnection(input);
			}

			// Get auxiliary statements generated during input resolution (like inline variable declarations)
			// These need to be added BEFORE the main statement
			var auxiliaryStatements = _context.GetAndClearAuxiliaryStatements();
			statements.AddRange(auxiliaryStatements);

			try
			{
				// Generate the statement for this node
				var statement = node.GenerateRoslynStatement(chunk.SubChunk, _context);

				// In debug builds, ALWAYS add #line directive for every node (not just those with breakpoints)
				// This allows breakpoints to be set dynamically during debugging
				// Create a #line directive that maps this statement to a unique virtual line
				// The virtual line encodes the node's execution order: 10000 + (order * 1000)
				int nodeVirtualLine = 10000 + (nodeExecutionOrder * 1000);
				
				// Format: #line 10000 "virtual_file.cs"
				var lineDirective = SF.Trivia(
					SF.LineDirectiveTrivia(
						SF.Token(SyntaxKind.HashToken),
						SF.Token(SyntaxKind.LineKeyword),
						SF.Literal(nodeVirtualLine),
						SF.Literal($"\"{virtualFileName}\"", virtualFileName), // Quoted filename
						SF.Token(SyntaxKind.EndOfDirectiveToken),
						true
					)
				);
				
				// Add the #line directive before the statement
				statement = statement.WithLeadingTrivia(lineDirective);
				
				// Record the mapping for this node (regardless of whether it currently has a breakpoint)
				// This allows breakpoints to be added dynamically after build
				_context.BreakpointMappings.Add(new NodeDev.Core.Debugger.NodeBreakpointInfo
				{
					NodeId = node.Id,
					NodeName = node.Name,
					ClassName = className,
					MethodName = methodName,
					LineNumber = nodeVirtualLine,
					SourceFile = virtualFileName
				});

				// Add the main statement
				statements.Add(statement);
				
				// Increment execution order for next node
				nodeExecutionOrder++;
			}
			catch (Exception ex) when (ex is not BuildError)
			{
				throw new BuildError($"Failed to generate statement for node type {node.GetType().Name}: {ex.Message}", node, ex);
			}
		}

		return statements;
	}
	
	/// <summary>
	/// Counts the number of lines a statement will take when normalized.
	/// This is a rough estimate used for line number tracking.
	/// </summary>
	private static int CountStatementLines(StatementSyntax statement)
	{
		// Count the number of line breaks in the statement text
		var text = statement.NormalizeWhitespace().ToFullString();
		return text.Split('\n').Length;
	}

	/// <summary>
	/// Resolves an input connection, either from another node's output or from a constant/parameter
	/// </summary>
	private void ResolveInputConnection(Connection input)
	{
		if (input.Type.IsExec)
			return;

		// Check if already resolved
		if (_context.GetVariableName(input) != null)
			return;

		if (input.Connections.Count == 0)
		{
			// No connection - use textbox value or default
			if (!input.Type.AllowTextboxEdit || input.ParsedTextboxValue == null)
			{
				// Register as default value
				var defaultVarName = _context.GetUniqueName($"{input.Parent.Name}_{input.Name}_default");
				_context.RegisterVariableName(input, defaultVarName);

				// Add declaration: var <varName> = default(Type);
				var typeSyntax = RoslynHelpers.GetTypeSyntax(input.Type);
				var declarator = SF.VariableDeclarator(SF.Identifier(defaultVarName))
					.WithInitializer(SF.EqualsValueClause(
						SF.DefaultExpression(typeSyntax)));

				_context.AddAuxiliaryStatement(
					SF.LocalDeclarationStatement(
						SF.VariableDeclaration(SF.IdentifierName("var"))
							.WithVariables(SF.SingletonSeparatedList(declarator))));
			}
			else
			{
				// Register as constant value
				var constVarName = _context.GetUniqueName($"{input.Parent.Name}_{input.Name}_const");
				_context.RegisterVariableName(input, constVarName);

				// Create literal expression
				ExpressionSyntax constValue = input.ParsedTextboxValue switch
				{
					null => SF.LiteralExpression(SyntaxKind.NullLiteralExpression),
					bool b => SF.LiteralExpression(b ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression),
					int i => SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(i)),
					long l => SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(l)),
					float f => SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(f)),
					double d => SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(d)),
					string s => SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(s)),
					char c => SF.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SF.Literal(c)),
					_ => SF.DefaultExpression(RoslynHelpers.GetTypeSyntax(input.Type))
				};

				// Add declaration with constant
				var declarator = SF.VariableDeclarator(SF.Identifier(constVarName))
					.WithInitializer(SF.EqualsValueClause(constValue));

				_context.AddAuxiliaryStatement(
					SF.LocalDeclarationStatement(
						SF.VariableDeclaration(SF.IdentifierName("var"))
							.WithVariables(SF.SingletonSeparatedList(declarator))));
			}
		}
		else
		{
			var outputConnection = input.Connections[0];
			var otherNode = outputConnection.Parent;

			if (otherNode.CanBeInlined)
			{
				// Check if this output was already generated
				var existingVarName = _context.GetVariableName(outputConnection);
				if (existingVarName != null)
				{
					// Reuse the existing variable
					_context.RegisterVariableName(input, existingVarName);
					return;
				}

				// Generate inline expression
				var inlineExpr = GenerateInlineExpression(otherNode);

				// Create a variable to hold the result
				var inlineVarName = _context.GetUniqueName($"{otherNode.Name}_{outputConnection.Name}");

				// Register the variable for BOTH the input and the output
				// This ensures other inputs that use the same output can find it
				_context.RegisterVariableName(input, inlineVarName);
				_context.RegisterVariableName(outputConnection, inlineVarName);

				// Add declaration: var <varName> = <inlineExpr>;
				var declarator = SF.VariableDeclarator(SF.Identifier(inlineVarName))
					.WithInitializer(SF.EqualsValueClause(inlineExpr));

				_context.AddAuxiliaryStatement(
					SF.LocalDeclarationStatement(
						SF.VariableDeclaration(SF.IdentifierName("var"))
							.WithVariables(SF.SingletonSeparatedList(declarator))));
			}
			else
			{
				// Use the pre-declared variable from the other node
				var varName = _context.GetVariableName(outputConnection);
				if (varName == null)
					throw new Exception($"Variable not found for connection {outputConnection.Name} of node {otherNode.Name}");

				_context.RegisterVariableName(input, varName);
			}
		}
	}

	/// <summary>
	/// Generates an inline expression for a node that can be inlined
	/// </summary>
	private ExpressionSyntax GenerateInlineExpression(Node node)
	{
		if (!node.CanBeInlined)
			throw new Exception($"Node {node.Name} cannot be inlined");

		// Resolve all inputs recursively
		foreach (var input in node.Inputs)
		{
			ResolveInputConnection(input);
		}

		try
		{
			return node.GenerateRoslynExpression(_context);
		}
		catch (Exception ex) when (ex is not BuildError)
		{
			throw new BuildError($"Failed to generate inline expression for node type {node.GetType().Name}: {ex.Message}", node, ex);
		}
	}

	/// <summary>
	/// Gets an expression for an input connection (either variable or parameter name)
	/// </summary>
	public ExpressionSyntax GetInputExpression(Connection input, GenerationContext context)
	{
		if (input.Type.IsExec)
			throw new ArgumentException("Cannot get expression for exec connection");

		var varName = context.GetVariableName(input);

		// If not found, check if it's a method parameter
		if (varName == null)
		{
			var param = _graph.SelfMethod.Parameters.FirstOrDefault(p => p.Name == input.Name);
			if (param != null)
				return SF.IdentifierName(param.Name);

			throw new Exception($"Variable name not found for connection {input.Name} of node {input.Parent.Name}");
		}

		return SF.IdentifierName(varName);
	}
}
