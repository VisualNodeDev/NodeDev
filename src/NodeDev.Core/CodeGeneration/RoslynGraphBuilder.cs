using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

		// Register method parameters in context
		foreach (var parameter in method.Parameters)
		{
			if (!parameter.ParameterType.IsExec)
			{
				var paramName = _context.GetUniqueName(parameter.Name);
				// Method parameters don't have a connection to register
				// They'll be referenced directly by name
			}
		}

		// Pre-declare variables for node outputs (similar to old CreateOutputsLocalVariableExpressions)
		var variableDeclarations = new List<LocalDeclarationStatementSyntax>();
		foreach (var node in _graph.Nodes.Values)
		{
			if (node.CanBeInlined)
				continue; // inline nodes don't need pre-declared variables

			foreach (var output in node.Outputs)
			{
				if (output.Type.IsExec)
					continue;

				var varName = _context.GetUniqueName($"{node.Name}_{output.Name}");
				_context.RegisterVariableName(output, varName);
				
				// Declare: var <varName>;
				variableDeclarations.Add(
					SyntaxHelper.CreateVarDeclaration(varName, SyntaxHelper.Default()));
			}
		}

		// Build the execution flow starting from entry
		var chunks = _graph.GetChunks(entryOutput, allowDeadEnd: false);
		var bodyStatements = BuildStatements(chunks);

		// Combine variable declarations with body statements
		var allStatements = variableDeclarations.Cast<StatementSyntax>()
			.Concat(bodyStatements)
			.ToList();

		// Add return statement if needed
		if (!method.HasReturnValue)
		{
			allStatements.Add(ReturnStatement());
		}

		// Create the method declaration
		var modifiers = new List<SyntaxToken>();
		modifiers.Add(Token(SyntaxKind.PublicKeyword));
		if (method.IsStatic)
			modifiers.Add(Token(SyntaxKind.StaticKeyword));

		var returnType = method.HasReturnValue 
			? SyntaxHelper.GetTypeSyntax(method.ReturnType)
			: PredefinedType(Token(SyntaxKind.VoidKeyword));

		var parameters = method.Parameters
			.Where(p => !p.ParameterType.IsExec)
			.Select(p => Parameter(Identifier(p.Name))
				.WithType(SyntaxHelper.GetTypeSyntax(p.ParameterType)));

		var methodDeclaration = MethodDeclaration(returnType, Identifier(method.Name))
			.WithModifiers(TokenList(modifiers))
			.WithParameterList(ParameterList(SeparatedList(parameters)))
			.WithBody(Block(allStatements));

		return methodDeclaration;
	}

	/// <summary>
	/// Builds statements from node path chunks
	/// </summary>
	public List<StatementSyntax> BuildStatements(Graph.NodePathChunks chunks)
	{
		var statements = new List<StatementSyntax>();

		foreach (var chunk in chunks.Chunks)
		{
			// Resolve inputs first
			foreach (var input in chunk.Input.Parent.Inputs)
			{
				ResolveInputConnection(input);
			}

			try
			{
				// Generate the statement for this node
				var statement = chunk.Input.Parent.GenerateRoslynStatement(chunk.SubChunk, _context);
				
				// Add any auxiliary statements first
				statements.AddRange(_context.GetAndClearAuxiliaryStatements());
				
				// Add the main statement
				statements.Add(statement);
			}
			catch (Exception ex) when (ex is not BuildError)
			{
				throw new BuildError(ex.Message, chunk.Input.Parent, ex);
			}
		}

		return statements;
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
				
				// Add declaration
				var defaultValue = SyntaxHelper.Default(SyntaxHelper.GetTypeSyntax(input.Type));
				_context.AddAuxiliaryStatement(
					SyntaxHelper.CreateVarDeclaration(defaultVarName, defaultValue));
			}
			else
			{
				// Register as constant value
				var constVarName = _context.GetUniqueName($"{input.Parent.Name}_{input.Name}_const");
				_context.RegisterVariableName(input, constVarName);
				
				// Add declaration with constant
				var constValue = SyntaxHelper.GetLiteralExpression(input.ParsedTextboxValue, input.Type);
				_context.AddAuxiliaryStatement(
					SyntaxHelper.CreateVarDeclaration(constVarName, constValue));
			}
		}
		else
		{
			var outputConnection = input.Connections[0];
			var otherNode = outputConnection.Parent;

			if (otherNode.CanBeInlined)
			{
				// Generate inline expression
				var inlineExpr = GenerateInlineExpression(otherNode);
				
				// Create a variable to hold the result
				var inlineVarName = _context.GetUniqueName($"{otherNode.Name}_{outputConnection.Name}");
				_context.RegisterVariableName(input, inlineVarName);
				
				// Add auxiliary statements from inline generation
				// Add declaration
				_context.AddAuxiliaryStatement(
					SyntaxHelper.CreateVarDeclaration(inlineVarName, inlineExpr));
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
			throw new BuildError(ex.Message, node, ex);
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
				return SyntaxHelper.Identifier(param.Name);
			
			throw new Exception($"Variable name not found for connection {input.Name} of node {input.Parent.Name}");
		}

		return SyntaxHelper.Identifier(varName);
	}
}
