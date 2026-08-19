using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.Connections;
using NodeDev.Core.Debugger;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Delegates;
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
	/// Gets the variable mappings collected during code generation.
	/// </summary>
	public List<ConnectionVariableMapping> GetVariableMappings() => _context.VariableMappings;

	/// <summary>
	/// Builds a complete method syntax from the graph
	/// </summary>
	public MethodDeclarationSyntax BuildMethod()
	{
		var method = _graph.SelfMethod;
		_graph.ValidateCallableScopes();

		// Set the current method in context for variable mapping
		string fullClassName = $"{_graph.SelfClass.Namespace}.{_graph.SelfClass.Name}";
		_context.SetCurrentMethod(fullClassName, method.Name);

		// Find the method entry only. Lambda entries and any corrupt method entry in a
		// child callable scope must not become the containing method's start point.
		var entryNodes = _graph.Nodes.Values
			.OfType<EntryNode>()
			.Where(x => x.CallableScopeId == null)
			.ToList();
		if (entryNodes.Count != 1)
			throw new Exception($"Expected exactly one root entry node in graph {method.Name}, but found {entryNodes.Count}.");
		var entryNode = entryNodes[0];

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

		var variableDeclarations = PredeclareOutputLocals(null, entryNode, _context);

		// Build the execution flow starting from entry
		var chunks = _graph.GetChunks(entryOutput, allowDeadEnd: false);
		
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
	/// Builds an explicitly typed, block-bodied lambda for a delegate creation node.
	/// Capture snapshots are deliberately queued in the containing context, while
	/// all body symbols and auxiliary statements live in a lexical child context.
	/// </summary>
	internal ExpressionSyntax BuildLambdaExpression(CreateDelegateNode delegateNode)
	{
		ValidateNodeScope(delegateNode);

		var delegateType = delegateNode.DelegateType;
		if (delegateType.HasUndefinedGenerics)
		{
			throw new BuildError(
				$"Delegate signature {delegateType.FriendlyName} contains unresolved generic types.",
				delegateNode,
				null);
		}

		var bodyNodes = _graph.Nodes.Values
			.Where(x => x.CallableScopeId == delegateNode.BodyScopeId)
			.ToList();
		var entries = bodyNodes.OfType<LambdaEntryNode>().ToList();
		if (entries.Count != 1)
		{
			throw new BuildError(
				$"Delegate {delegateNode.SignatureDisplayName} requires exactly one lambda entry, but found {entries.Count}.",
				delegateNode,
				null);
		}

		if (bodyNodes.OfType<EntryNode>().Any() || bodyNodes.OfType<ReturnNode>().Any())
		{
			throw new BuildError(
				$"Method entry and return nodes are not valid inside {delegateNode.SignatureDisplayName}.",
				delegateNode,
				null);
		}

		if (delegateNode.Kind == DelegateKind.Action && bodyNodes.OfType<LambdaReturnNode>().Any())
			throw new BuildError("An Action lambda cannot contain a lambda return node.", delegateNode, null);
		if (delegateNode.Kind == DelegateKind.Func && bodyNodes.OfType<LambdaCompleteNode>().Any())
			throw new BuildError("A Func lambda cannot contain a lambda completion node.", delegateNode, null);

		var entry = entries[0];
		var expectedEntryOutputCount = 1 + delegateNode.Parameters.Count + delegateNode.Captures.Count;
		if (entry.Outputs.Count != expectedEntryOutputCount || !entry.Outputs[0].Type.IsExec)
		{
			throw new BuildError(
				$"Lambda entry ports do not match delegate signature {delegateNode.SignatureDisplayName}.",
				entry,
				null);
		}

		var childContext = _context.CreateChild(delegateNode.BodyScopeId);
		var lambdaParameters = new List<ParameterSyntax>(delegateNode.Parameters.Count);
		for (var index = 0; index < delegateNode.Parameters.Count; index++)
		{
			var definition = delegateNode.Parameters[index];
			var parameterName = childContext.GetUniqueName(definition.Name);
			childContext.RegisterVariableName(entry.Outputs[index + 1], parameterName);
			lambdaParameters.Add(
				SF.Parameter(SF.Identifier(parameterName))
					.WithType(RoslynHelpers.GetTypeSyntax(definition.Type)));
		}

		for (var index = 0; index < delegateNode.Captures.Count; index++)
		{
			var capture = delegateNode.Captures[index];
			var captureInput = delegateNode.CaptureInputs[index];
			ResolveInputConnection(captureInput);
			var outerVariableName = _context.GetVariableName(captureInput)
				?? throw new BuildError($"Unable to resolve capture {capture.Name}.", delegateNode, null);

			var snapshotName = _context.GetUniqueName($"lambdaCapture_{capture.Name}");
			var snapshotDeclarator = SF.VariableDeclarator(SF.Identifier(snapshotName))
				.WithInitializer(SF.EqualsValueClause(SF.IdentifierName(outerVariableName)));
			_context.AddAuxiliaryStatement(
				SF.LocalDeclarationStatement(
					SF.VariableDeclaration(SF.IdentifierName("var"))
						.WithVariables(SF.SingletonSeparatedList(snapshotDeclarator))));

			var entryOutputIndex = 1 + delegateNode.Parameters.Count + index;
			childContext.RegisterVariableName(entry.Outputs[entryOutputIndex], snapshotName);
		}

		var childBuilder = new RoslynGraphBuilder(_graph, childContext);
		var body = childBuilder.BuildCallableBody(delegateNode.BodyScopeId, entry);
		var lambda = SF.ParenthesizedLambdaExpression()
			.WithParameterList(SF.ParameterList(SF.SeparatedList(lambdaParameters)))
			.WithBlock(body);

		return SF.CastExpression(
			RoslynHelpers.GetExactDelegateTypeSyntax(delegateType),
			SF.ParenthesizedExpression(lambda));
	}

	/// <summary>
	/// Builds the statements and local declarations for one non-method callable body.
	/// </summary>
	internal BlockSyntax BuildCallableBody(string callableScopeId, LambdaEntryNode entryNode)
	{
		if (_context.CallableScopeId != callableScopeId || entryNode.CallableScopeId != callableScopeId)
		{
			throw new BuildError(
				$"Lambda entry {entryNode.Name} does not belong to callable scope '{callableScopeId}'.",
				entryNode,
				null);
		}

		var entryOutput = entryNode.Outputs.SingleOrDefault(x => x.Type.IsExec)
			?? throw new BuildError("Lambda entry has no execution output.", entryNode, null);
		var variableDeclarations = PredeclareOutputLocals(callableScopeId, entryNode, _context);
		var chunks = _graph.GetChunks(entryOutput, allowDeadEnd: false);
		var bodyStatements = BuildStatements(chunks);

		return SF.Block(variableDeclarations.Cast<StatementSyntax>().Concat(bodyStatements));
	}

	private List<LocalDeclarationStatementSyntax> PredeclareOutputLocals(
		string? callableScopeId,
		Node entryNode,
		GenerationContext context)
	{
		var variableDeclarations = new List<LocalDeclarationStatementSyntax>();
		foreach (var node in _graph.Nodes.Values.Where(x => x.CallableScopeId == callableScopeId))
		{
			if (node.CanBeInlined || node == entryNode)
				continue;

			foreach (var output in node.Outputs.Where(x => !x.Type.IsExec))
			{
				var varName = context.GetUniqueName($"{node.Name}_{output.Name}");
				context.RegisterVariableName(output, varName);

				var declarator = SF.VariableDeclarator(SF.Identifier(varName))
					.WithInitializer(SF.EqualsValueClause(
						SF.DefaultExpression(RoslynHelpers.GetTypeSyntax(output.Type))));

				variableDeclarations.Add(
					SF.LocalDeclarationStatement(
						SF.VariableDeclaration(SF.IdentifierName("var"))
							.WithVariables(SF.SingletonSeparatedList(declarator))));
			}
		}

		return variableDeclarations;
	}

	/// <summary>
	/// Builds statements from node path chunks
	/// </summary>
	internal List<StatementSyntax> BuildStatements(Graph.NodePathChunks chunks)
	{
		return BuildStatementsCore(
			chunks,
			_context.IsDebug,
			_context.IsDebug ? _context.CurrentClassName : null,
			_context.IsDebug ? _context.CurrentMethodName : null);
	}
	
	/// <summary>
	/// Builds statements from node path chunks, tracking line numbers for breakpoints.
	/// Returns the statements and populates breakpoint info in the context.
	/// </summary>
	internal List<StatementSyntax> BuildStatementsWithBreakpointTracking(Graph.NodePathChunks chunks, string className, string methodName)
	{
		_context.SetCurrentMethod(className, methodName);
		return BuildStatementsCore(chunks, true, className, methodName);
	}

	private List<StatementSyntax> BuildStatementsCore(
		Graph.NodePathChunks chunks,
		bool trackBreakpoints,
		string? className,
		string? methodName)
	{
		var statements = new List<StatementSyntax>();
		ValidateNodeScope(chunks.OutputStartPoint.Parent);
		var virtualFileName = trackBreakpoints
			? $"NodeDev_{className}_{methodName}.g.cs"
			: null;

		foreach (var chunk in chunks.Chunks)
		{
			var node = chunk.Input.Parent;
			ValidateNodeScope(node);
			
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
				// Allocate before recursively generating compound statements so parent and
				// nested nodes share one stable, method-wide sequence.
				var nodeVirtualLine = trackBreakpoints
					? _context.AllocateVirtualLine()
					: 0;

				// Generate the statement for this node
				var statement = node.GenerateRoslynStatement(chunk.SubChunk, _context);

				if (trackBreakpoints)
				{
					var lineDirective = SF.Trivia(
						SF.LineDirectiveTrivia(
							SF.Token(SyntaxKind.HashToken),
							SF.Token(SyntaxKind.LineKeyword),
							SF.Literal(nodeVirtualLine),
							SF.Literal($"\"{virtualFileName}\"", virtualFileName!),
							SF.Token(SyntaxKind.EndOfDirectiveToken),
							true));

					statement = statement.WithLeadingTrivia(lineDirective);
					_context.BreakpointMappings.Add(new NodeBreakpointInfo
					{
						NodeId = node.Id,
						NodeName = node.Name,
						ClassName = className!,
						MethodName = methodName!,
						LineNumber = nodeVirtualLine,
						SourceFile = virtualFileName!
					});
				}

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

	private void ValidateNodeScope(Node node)
	{
		if (node.CallableScopeId != _context.CallableScopeId)
		{
			throw new BuildError(
				$"Node {node.Name} belongs to callable scope '{node.CallableScopeId ?? "method"}' but was reached while building '{_context.CallableScopeId ?? "method"}'.",
				node,
				null);
		}
	}

	/// <summary>
	/// Resolves an input connection, either from another node's output or from a constant/parameter
	/// </summary>
	private void ResolveInputConnection(Connection input)
	{
		ValidateNodeScope(input.Parent);

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
			ValidateNodeScope(otherNode);

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

		if (varName == null)
			throw new Exception($"Variable name not found for connection {input.Name} of node {input.Parent.Name}");

		return SF.IdentifierName(varName);
	}
}
