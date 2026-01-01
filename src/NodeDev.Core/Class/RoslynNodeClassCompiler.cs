using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using NodeDev.Core.Class;
using NodeDev.Core.CodeGeneration;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Class;

/// <summary>
/// Roslyn-based class compiler for NodeDev projects
/// </summary>
public class RoslynNodeClassCompiler
{
	private readonly Project _project;
	private readonly BuildOptions _options;

	public RoslynNodeClassCompiler(Project project, BuildOptions options)
	{
		_project = project;
		_options = options;
	}

	/// <summary>
	/// Compiles the project classes using Roslyn
	/// </summary>
	public CompilationResult Compile()
	{
		// Generate the compilation unit (full source code)
		var compilationUnit = GenerateCompilationUnit();

		// Normalize whitespace for proper debugging
		compilationUnit = (CompilationUnitSyntax)compilationUnit.NormalizeWhitespace();

		// Convert to source text
		var sourceText = compilationUnit.ToFullString();

		// Create syntax tree with embedded text for debugging
		var syntaxTree = CSharpSyntaxTree.ParseText(
			sourceText,
			new CSharpParseOptions(LanguageVersion.Latest),
			path: $"NodeDev_{_project.Id}.cs",
			encoding: Encoding.UTF8);

		// Add references to required assemblies
		var references = GetMetadataReferences();

		// Create compilation
		var assemblyName = $"NodeProject_{_project.Id.ToString().Replace('-', '_')}";
		var compilation = CSharpCompilation.Create(
			assemblyName,
			syntaxTrees: new[] { syntaxTree },
			references: references,
			options: new CSharpCompilationOptions(
				OutputKind.DynamicallyLinkedLibrary,
				optimizationLevel: _options.BuildExpressionOptions.RaiseNodeExecutedEvents 
					? OptimizationLevel.Debug 
					: OptimizationLevel.Release,
				platform: Platform.AnyCpu,
				allowUnsafe: false));

		// Emit to memory
		using var peStream = new MemoryStream();
		using var pdbStream = new MemoryStream();

		// Embed source text for debugging
		var embeddedTexts = new[] { EmbeddedText.FromSource(syntaxTree.FilePath, SourceText.From(sourceText, Encoding.UTF8)) };

		var emitOptions = new EmitOptions(
			debugInformationFormat: DebugInformationFormat.PortablePdb,
			pdbFilePath: $"{assemblyName}.pdb");

		var emitResult = compilation.Emit(
			peStream,
			pdbStream,
			embeddedTexts: embeddedTexts,
			options: emitOptions);

		if (!emitResult.Success)
		{
			var errors = emitResult.Diagnostics
				.Where(d => d.Severity == DiagnosticSeverity.Error)
				.Select(d => $"{d.Id}: {d.GetMessage()}")
				.ToList();

			throw new CompilationException($"Compilation failed:\n{string.Join("\n", errors)}", errors, sourceText);
		}

		// Load the assembly
		peStream.Seek(0, SeekOrigin.Begin);
		pdbStream.Seek(0, SeekOrigin.Begin);

		var assembly = Assembly.Load(peStream.ToArray(), pdbStream.ToArray());

		return new CompilationResult(assembly, sourceText, peStream.ToArray(), pdbStream.ToArray());
	}

	/// <summary>
	/// Generates the full compilation unit with all classes
	/// </summary>
	private CompilationUnitSyntax GenerateCompilationUnit()
	{
		var namespaceDeclarations = new List<MemberDeclarationSyntax>();

		// Group classes by namespace
		var classGroups = _project.Classes.GroupBy(c => c.Namespace);

		foreach (var group in classGroups)
		{
			var classDeclarations = new List<MemberDeclarationSyntax>();

			foreach (var nodeClass in group)
			{
				classDeclarations.Add(GenerateClass(nodeClass));
			}

			// Create namespace declaration
			var namespaceDecl = SF.FileScopedNamespaceDeclaration(SF.ParseName(group.Key))
				.WithMembers(SF.List(classDeclarations));

			namespaceDeclarations.Add(namespaceDecl);
		}

		// Create compilation unit with usings
		var compilationUnit = SF.CompilationUnit()
			.WithUsings(SF.List(new[]
			{
				SF.UsingDirective(SF.ParseName("System")),
				SF.UsingDirective(SF.ParseName("System.Collections.Generic")),
				SF.UsingDirective(SF.ParseName("System.Linq")),
			}))
			.WithMembers(SF.List(namespaceDeclarations));

		return compilationUnit;
	}

	/// <summary>
	/// Generates a class declaration
	/// </summary>
	private ClassDeclarationSyntax GenerateClass(NodeClass nodeClass)
	{
		var members = new List<MemberDeclarationSyntax>();

		// Generate properties
		foreach (var property in nodeClass.Properties)
		{
			members.Add(GenerateProperty(property));
		}

		// Generate methods
		foreach (var method in nodeClass.Methods)
		{
			members.Add(GenerateMethod(method));
		}

		// Create class declaration
		var classDecl = SF.ClassDeclaration(nodeClass.Name)
			.WithModifiers(SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)))
			.WithMembers(SF.List(members));

		return classDecl;
	}

	/// <summary>
	/// Generates a property declaration
	/// </summary>
	private PropertyDeclarationSyntax GenerateProperty(NodeClassProperty property)
	{
		var propertyType = RoslynHelpers.GetTypeSyntax(property.PropertyType);

		var propertyDecl = SF.PropertyDeclaration(propertyType, property.Name)
			.WithModifiers(SF.TokenList(SF.Token(SyntaxKind.PublicKeyword)))
			.WithAccessorList(SF.AccessorList(SF.List(new[]
			{
				SF.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
					.WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken)),
				SF.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
					.WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken))
			})));

		return propertyDecl;
	}

	/// <summary>
	/// Generates a method declaration
	/// </summary>
	private MethodDeclarationSyntax GenerateMethod(NodeClassMethod method)
	{
		var builder = new RoslynGraphBuilder(method.Graph, _options.BuildExpressionOptions.RaiseNodeExecutedEvents);
		return builder.BuildMethod();
	}

	/// <summary>
	/// Gets metadata references for compilation
	/// </summary>
	private List<MetadataReference> GetMetadataReferences()
	{
		var references = new List<MetadataReference>();

		// Add core runtime assemblies
		references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
		references.Add(MetadataReference.CreateFromFile(typeof(Console).Assembly.Location));
		references.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));

		// Add System.Runtime
		var systemRuntimeAssembly = Assembly.Load("System.Runtime");
		references.Add(MetadataReference.CreateFromFile(systemRuntimeAssembly.Location));

		// Add System.Collections
		var collectionsAssembly = Assembly.Load("System.Collections");
		references.Add(MetadataReference.CreateFromFile(collectionsAssembly.Location));

		return references;
	}

	/// <summary>
	/// Result of a Roslyn compilation
	/// </summary>
	public record CompilationResult(Assembly Assembly, string SourceCode, byte[] PEBytes, byte[] PDBBytes);

	/// <summary>
	/// Exception thrown when compilation fails
	/// </summary>
	public class CompilationException : Exception
	{
		public List<string> Errors { get; }
		public string SourceCode { get; }

		public CompilationException(string message, List<string> errors, string sourceCode) : base(message)
		{
			Errors = errors;
			SourceCode = sourceCode;
		}
	}
}
