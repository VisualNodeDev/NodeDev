using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.ManagerServices;
using NodeDev.Core.Migrations;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Nodes;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NodeDev.Tests")]

namespace NodeDev.Core;

public class Project
{
	internal record class SerializedProject(Guid Id, string NodeDevVersion, List<NodeClass.SerializedNodeClass> Classes, ProjectSettings Settings);

	internal readonly Guid Id;

	internal readonly string NodeDevVersion;

	public static string CurrentNodeDevVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? throw new Exception("Unable to get current NodeDev version");

	private readonly List<NodeClass> _Classes = [];
	public IReadOnlyList<NodeClass> Classes => _Classes;

	private readonly Dictionary<NodeClass, NodeClassType> NodeClassTypes = [];

	public readonly TypeFactory TypeFactory;

	public ProjectSettings Settings { get; private set; } = ProjectSettings.Default;

	public NodeClassTypeCreator? NodeClassTypeCreator { get; private set; }

	public GraphExecutor? GraphExecutor { get; set; }

	internal Subject<(Graph Graph, bool RequireUIRefresh)> GraphChangedSubject { get; } = new();

	internal Subject<(GraphExecutor Executor, Node Node, Connection Exec)> GraphNodeExecutingSubject { get; } = new();
	internal Subject<(GraphExecutor Executor, Node Node, Connection Exec)> GraphNodeExecutedSubject { get; } = new();
	internal Subject<bool> GraphExecutionChangedSubject { get; } = new();
	internal Subject<string> ConsoleOutputSubject { get; } = new();

	public IObservable<(Graph Graph, bool RequireUIRefresh)> GraphChanged => GraphChangedSubject.AsObservable();

	public IObservable<(GraphExecutor Executor, Node Node, Connection Exec)> GraphNodeExecuting => GraphNodeExecutingSubject.AsObservable();

	public IObservable<(GraphExecutor Executor, Node Node, Connection Exec)> GraphNodeExecuted => GraphNodeExecutedSubject.AsObservable();

	public IObservable<bool> GraphExecutionChanged => GraphExecutionChangedSubject.AsObservable();

	public IObservable<string> ConsoleOutput => ConsoleOutputSubject.AsObservable();

	public bool IsLiveDebuggingEnabled { get; private set; }

	public Project(Guid id, string? nodeDevVersion = null)
	{
		Id = id;
		NodeDevVersion = nodeDevVersion ?? CurrentNodeDevVersion;
		TypeFactory = new TypeFactory(this);
	}

	public IEnumerable<T> GetNodes<T>() where T : Node
	{
		return Classes.SelectMany(x => x.Methods).SelectMany(x => x.Graph.Nodes.Values).OfType<T>();
	}

	#region CreateNewDefaultProject

	public static Project CreateNewDefaultProject()
	{
		return CreateNewDefaultProject(out _);
	}

	public static Project CreateNewDefaultProject(out NodeClassMethod main)
	{
		var project = new Project(Guid.NewGuid());

		var programClass = new NodeClass("Program", "NewProject", project);
		project.AddClass(programClass);

		// Create the main method and add it to the project
		main = new NodeClassMethod(programClass, "Main", project.TypeFactory.Get<int>(), true);
		programClass.AddMethod(main, createEntryAndReturn: true);

		// Now that the method is created with its entry and return nodes, we can set the default return value of 0
		main.ReturnNodes.First().Inputs[1].UpdateTextboxText("0");

		return project;
	}

	#endregion

	#region AddClass

	public void AddClass(NodeClass nodeClass)
	{
		_Classes.Add(nodeClass);
	}

	#endregion

	#region Build

	public string Build(BuildOptions buildOptions)
	{
		var name = "project";
		
		// Use Roslyn compilation
		var compiler = new RoslynNodeClassCompiler(this, buildOptions);
		var result = compiler.Compile();

		// Check if this is an executable (has a Program.Main method)
		bool isExecutable = HasMainMethod();

		Directory.CreateDirectory(buildOptions.OutputPath);
		var fileExtension = isExecutable ? ".exe" : ".dll";
		var filePath = Path.Combine(buildOptions.OutputPath, $"{name}{fileExtension}");
		var pdbPath = Path.Combine(buildOptions.OutputPath, $"{name}.pdb");

		// Write the PE and PDB to files
		File.WriteAllBytes(filePath, result.PEBytes);
		File.WriteAllBytes(pdbPath, result.PDBBytes);

		if (isExecutable)
		{
			// Create runtime config for executables
			File.WriteAllText(Path.Combine(buildOptions.OutputPath, $"{name}.runtimeconfig.json"), @$"{{
    ""runtimeOptions"": {{
        ""tfm"": ""net{Environment.Version.Major}.{Environment.Version.Minor}"",
        ""framework"": {{
            ""name"": ""Microsoft.NETCore.App"",
            ""version"": ""{GetNetCoreVersion()}""
        }}
    }}
}}");
		}

		return filePath;
	}

	/// <summary>
	/// Checks if the project has a static Main method in a Program class (indicating an executable)
	/// </summary>
	internal bool HasMainMethod()
	{
		var program = Classes.FirstOrDefault(x => x.Name == "Program");
		var mainMethod = program?.Methods.FirstOrDefault(x => x.Name == "Main" && x.IsStatic);
		return program != null && mainMethod != null;
	}

	private static string GetNetCoreVersion()
	{
		var assembly = typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly;
		var assemblyPath = assembly.Location.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
		int netCoreAppIndex = Array.IndexOf(assemblyPath, "Microsoft.NETCore.App");

		if (netCoreAppIndex > 0 && netCoreAppIndex < assemblyPath.Length - 2)
			return assemblyPath[netCoreAppIndex + 1];

		return "";
	}

	public AssemblyBuilder BuildAndGetAssembly(BuildOptions buildOptions)
	{
		CreateNodeClassTypeCreator(buildOptions);

		if (NodeClassTypeCreator == null)
			throw new NullReferenceException($"NodeClassTypeCreator should not be null after {nameof(CreateNodeClassTypeCreator)} was called, this shouldn't happen");

		NodeClassTypeCreator.CreateProjectClassesAndAssembly();

		var assembly = NodeClassTypeCreator.Assembly;
		if (assembly == null)
			throw new NullReferenceException("NodeClassTypeCreator.Assembly null after project was compiled, this shouldn't happen");

		return assembly;
	}

	/// <summary>
	/// Builds the project using Roslyn compilation (new approach)
	/// </summary>
	public Assembly BuildWithRoslyn(BuildOptions buildOptions)
	{
		var compiler = new RoslynNodeClassCompiler(this, buildOptions);
		var result = compiler.Compile();
		return result.Assembly;
	}

	#endregion

	#region Run

	/// <summary>
	/// Locates the ScriptRunner executable in the build output directory.
	/// ScriptRunner is a console application that serves as the target process for debugging.
	/// </summary>
	private static string FindScriptRunnerExecutable()
	{
		// Get the directory where NodeDev.Core assembly is located
		string coreAssemblyLocation = Assembly.GetExecutingAssembly().Location;
		string coreDirectory = Path.GetDirectoryName(coreAssemblyLocation) ?? throw new Exception("Unable to determine NodeDev.Core assembly directory");

		// ScriptRunner should be in the same build output directory
		string scriptRunnerDll = Path.Combine(coreDirectory, "NodeDev.ScriptRunner.dll");

		if (File.Exists(scriptRunnerDll))
		{
			return scriptRunnerDll;
		}

		// If not found in the same directory, try looking in sibling directories (for development scenarios)
		string? buildOutputRoot = Path.GetDirectoryName(coreDirectory);
		if (buildOutputRoot != null)
		{
			string scriptRunnerPath = Path.Combine(buildOutputRoot, "NodeDev.ScriptRunner", "NodeDev.ScriptRunner.dll");
			if (File.Exists(scriptRunnerPath))
			{
				return scriptRunnerPath;
			}
		}

		throw new FileNotFoundException("ScriptRunner executable not found. Please ensure NodeDev.ScriptRunner is built and available.");
	}

	/// <summary>
	/// Gets the path to the ScriptRunner executable.
	/// This is useful for debugging infrastructure that needs to locate the runner.
	/// </summary>
	/// <returns>The full path to NodeDev.ScriptRunner.dll</returns>
	public string GetScriptRunnerPath()
	{
		return FindScriptRunnerExecutable();
	}

	public object? Run(BuildOptions options, params object?[] inputs)
	{
		try
		{
			var assemblyPath = Build(options);

			// Find the ScriptRunner executable
			string scriptRunnerPath = FindScriptRunnerExecutable();
			
			// Convert to absolute path to avoid confusion with working directory
			string absoluteAssemblyPath = Path.GetFullPath(assemblyPath);
			
			// Build arguments: ScriptRunner.dll path-to-user-dll [user-args...]
			var userArgsString = string.Join(" ", inputs.Select(x => '"' + (x?.ToString() ?? "") + '"'));
			var arguments = $"\"{scriptRunnerPath}\" \"{absoluteAssemblyPath}\"";
			if (!string.IsNullOrEmpty(userArgsString))
			{
				arguments += $" {userArgsString}";
			}
			
			var processStartInfo = new System.Diagnostics.ProcessStartInfo()
			{
				FileName = "dotnet",
				Arguments = arguments,
				WorkingDirectory = Path.GetDirectoryName(absoluteAssemblyPath),
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};
			
			var process = System.Diagnostics.Process.Start(processStartInfo) ?? throw new Exception("Unable to start process");

			// Notify that execution has started
			GraphExecutionChangedSubject.OnNext(true);

			// Capture output and error streams
			process.OutputDataReceived += (sender, e) =>
			{
				if (e.Data != null)
				{
					ConsoleOutputSubject.OnNext(e.Data + Environment.NewLine);
				}
			};

			process.ErrorDataReceived += (sender, e) =>
			{
				if (e.Data != null)
				{
					ConsoleOutputSubject.OnNext(e.Data + Environment.NewLine);
				}
			};

			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			process.WaitForExit();

			// Notify that execution has completed
			GraphExecutionChangedSubject.OnNext(false);

			return process.ExitCode;
		}
		catch (Exception)
		{
			// Notify that execution has completed even on error
			GraphExecutionChangedSubject.OnNext(false);
			return null;
		}
		finally
		{
			NodeClassTypeCreator = null;
			GC.Collect();
		}
	}

	#endregion

	#region GetCreatedClassType / GetNodeClassType

	public NodeClassTypeCreator CreateNodeClassTypeCreator(BuildOptions options)
	{
		return NodeClassTypeCreator = new(this, options);
	}

	public Type GetCreatedClassType(NodeClass nodeClass)
	{
		if (NodeClassTypeCreator == null)
			throw new Exception("NodeClassTypeCreator is null, is the project currently being run?");

		if (NodeClassTypeCreator.GeneratedTypes.TryGetValue(GetNodeClassType(nodeClass), out var generatedType))
			return generatedType.Type;

		throw new Exception("Unable to get generated node class for provided class: " + nodeClass.Name);
	}

	public NodeClassType GetNodeClassType(NodeClass nodeClass, TypeBase[]? generics = null)
	{
		if (!NodeClassTypes.ContainsKey(nodeClass))
			return NodeClassTypes[nodeClass] = new(nodeClass, generics ?? Array.Empty<TypeBase>());
		return NodeClassTypes[nodeClass];
	}

	#endregion

	#region Serialize / Deserialize

	public string Serialize()
	{
		var serializedProject = new SerializedProject(Id, NodeDevVersion, Classes.Select(x => x.Serialize()).ToList(), Settings);

		return JsonSerializer.Serialize(serializedProject, new JsonSerializerOptions()
		{
			WriteIndented = true
		});
	}

	public static Project Deserialize(string serialized)
	{
		var document = JsonNode.Parse(serialized)?.AsObject() ?? throw new Exception("Unable to deserialize project");

		var migrations = GetMigrationsRequired(document);
		// Run the migrations that needs to change the literal JSON content before deserialization
		foreach (var migration in migrations)
			migration.PerformMigrationBeforeDeserialization(document);

		var serializedProject = document.Deserialize<SerializedProject>() ?? throw new Exception("Unable to deserialize project");
		var project = new Project(serializedProject.Id == default ? Guid.NewGuid() : serializedProject.Id);

		var nodeClasses = new Dictionary<NodeClass, NodeClass.SerializedNodeClass>();
		foreach (var nodeClassSerializedObj in serializedProject.Classes)
		{
			var nodeClass = NodeClass.Deserialize(nodeClassSerializedObj, project);
			project._Classes.Add(nodeClass);

			nodeClasses[nodeClass] = nodeClassSerializedObj;
		}

		// Run the migrations that need to alter the project now that it's deserialized as well as the classes
		foreach (var migration in migrations)
			migration.PerformMigrationAfterClassesDeserialization(project);

		// will deserialize methods and properties but not any graphs
		foreach (var nodeClass in nodeClasses)
			nodeClass.Key.Deserialize_Step2(nodeClass.Value);

		// deserialize graphs
		foreach (var nodeClass in nodeClasses)
			nodeClass.Key.Deserialize_Step3();


		return project;
	}

	#endregion

	#region Version Migrations

	private static List<MigrationBase> GetMigrationsRequired(JsonObject projectJson)
	{
		if (string.IsNullOrEmpty(projectJson["NodeDevVersion"]?.ToString()) || !NuGet.Versioning.NuGetVersion.TryParse(projectJson["NodeDevVersion"]?.ToString(), out var current))
		{
			current = NuGet.Versioning.NuGetVersion.Parse("1.0.0");
			projectJson["NodeDevVersion"] = "1.0.0";
		}

		return System.Reflection.Assembly.GetExecutingAssembly()
			.GetTypes()
			.Where(x => x.IsSubclassOf(typeof(MigrationBase)))
			.Select(x => (MigrationBase)Activator.CreateInstance(x)!)
			.Select(x => (Version: NuGet.Versioning.NuGetVersion.Parse(x.Version), Migration: x))
			.Where(x => x.Version > current)
			.OrderBy(x => x.Version)
			.Select(x => x.Migration)
			.ToList();
	}

	#endregion

	#region Debugging

	public void StopLiveDebugging()
	{
		IsLiveDebuggingEnabled = false;

		// TODO Search through the executors tree and free up all unused executors
	}

	public void StartLiveDebugging()
	{
		IsLiveDebuggingEnabled = true;
	}

	#endregion

}
