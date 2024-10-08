﻿using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.Migrations;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Text.Json.Nodes;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NodeDev.Tests")]

namespace NodeDev.Core;

public class Project
{
    internal record class SerializedProject(Guid Id, string NodeDevVersion, List<NodeClass.SerializedNodeClass> Classes, ProjectSettings Settings);

    internal readonly Guid Id;

    internal readonly string NodeDevVersion;

    public static string CurrentNodeDevVersion => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? throw new Exception("Unable to get current NodeDev version");

    public List<NodeClass> Classes { get; } = new();

    private Dictionary<NodeClass, NodeClassType> NodeClassTypes = new();

    public readonly TypeFactory TypeFactory;

    public ProjectSettings Settings { get; private set; } = ProjectSettings.Default;

    public NodeClassTypeCreator? NodeClassTypeCreator { get; private set; }

    public GraphExecutor? GraphExecutor { get; set; }

    internal Subject<(Graph Graph, bool RequireUIRefresh)> GraphChangedSubject { get; } = new();

    internal Subject<(GraphExecutor Executor, Node Node, Connection Exec)> GraphNodeExecutingSubject { get; } = new();
    internal Subject<(GraphExecutor Executor, Node Node, Connection Exec)> GraphNodeExecutedSubject { get; } = new();
    internal Subject<bool> GraphExecutionChangedSubject { get; } = new();

    public IObservable<(Graph Graph, bool RequireUIRefresh)> GraphChanged => GraphChangedSubject.AsObservable();

    public IObservable<(GraphExecutor Executor, Node Node, Connection Exec)> GraphNodeExecuting => GraphNodeExecutingSubject.AsObservable();

    public IObservable<(GraphExecutor Executor, Node Node, Connection Exec)> GraphNodeExecuted => GraphNodeExecutedSubject.AsObservable();

    public IObservable<bool> GraphExecutionChanged => GraphExecutionChangedSubject.AsObservable();

    public bool IsLiveDebuggingEnabled { get; private set; }

    public Project(Guid id, string? nodeDevVersion = null)
    {
        Id = id;
        TypeFactory = new TypeFactory(this);
        NodeDevVersion = nodeDevVersion ?? CurrentNodeDevVersion;
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

        main = new NodeClassMethod(programClass, "Main", project.TypeFactory.Get(typeof(void), null), new Graph());
        main.IsStatic = true;

		var entry = new EntryNode(main.Graph);
		var returnNode = new ReturnNode(main.Graph);
		main.Graph.AddNode(entry, false);
        main.Graph.AddNode(returnNode, false);
        programClass.Methods.Add(main);
		project.Classes.Add(programClass);

		// Connect entry's exec to return node's exec
		main.Graph.Connect(entry.Outputs[0], returnNode.Inputs[0], false);

        return project;
    }

    #endregion

    #region Run

    public object? Run(BuildOptions options, params object?[] inputs)
    {
        try
        {
            CreateNodeClassTypeCreator(options);

            if (NodeClassTypeCreator == null)
                throw new NullReferenceException($"NodeClassTypeCreator should not be null after {nameof(CreateNodeClassTypeCreator)} was called, this shouldn't happen");

            NodeClassTypeCreator.CreateProjectClassesAndAssembly();

            var assembly = NodeClassTypeCreator.Assembly ?? throw new NullReferenceException("NodeClassTypeCreator.Assembly null after project was compiled, this shouldn't happen");
            var program = assembly.CreateInstance("Program")!;

            GraphExecutionChangedSubject.OnNext(true);

            var main = program.GetType().GetMethod("Main")!;
            return main.Invoke(program, inputs);
        }
        catch (Exception ex)
        {
            return null;
        }
        finally
        {
            NodeClassTypeCreator = null;
            GC.Collect();

            GraphExecutionChangedSubject.OnNext(false);
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
            project.Classes.Add(nodeClass);

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
