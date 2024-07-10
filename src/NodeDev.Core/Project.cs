using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection.Emit;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NodeDev.Tests")]

namespace NodeDev.Core;

public class Project
{
	private record class SerializedProject(Guid Id, List<string> Classes);

	internal readonly Guid Id;

	public List<NodeClass> Classes { get; } = new();

	private Dictionary<NodeClass, NodeClassType> NodeClassTypes = new();

	public readonly TypeFactory TypeFactory;

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

	public Project(Guid id)
	{
		Id = id;
		TypeFactory = new TypeFactory(this);
	}

	public IEnumerable<T> GetNodes<T>() where T : Node
	{
		return Classes.SelectMany(x => x.Methods).SelectMany(x => x.Graph.Nodes.Values).OfType<T>();
	}

	#region CreateNewDefaultProject

	public static Project CreateNewDefaultProject()
	{
		var project = new Project(Guid.NewGuid());

		var programClass = new NodeClass("Program", "NewProject", project);

		var main = new NodeClassMethod(programClass, "Main", project.TypeFactory.Get(typeof(void), null), new Graph());
		main.IsStatic = true;

		main.Graph.AddNode(new EntryNode(main.Graph), false);
		main.Graph.AddNode(new ReturnNode(main.Graph), false);
		programClass.Methods.Add(main);

		project.Classes.Add(programClass);

		return project;
	}

	#endregion

	#region Run

    public object? Run(BuildOptions options, params object?[] inputs)
    {
		try
		{
			CreateNodeClassTypeCreator(options);

			if(NodeClassTypeCreator == null)
				throw new NullReferenceException($"NodeClassTypeCreator should not be null after {nameof(CreateNodeClassTypeCreator)} was called, this shouldn't happen");

            NodeClassTypeCreator.CreateProjectClassesAndAssembly();

			var assembly = NodeClassTypeCreator.Assembly ?? throw new NullReferenceException("NodeClassTypeCreator.Assembly null after project was compiled, this shouldn't happen");
			var program = assembly.CreateInstance("Program")!;

			GraphExecutionChangedSubject.OnNext(true);

			var main = program.GetType().GetMethod("Main")!;
			return main.Invoke(program, inputs);
		}
		catch(Exception ex)
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
		var serializedProject = new SerializedProject(Id, Classes.Select(x => x.Serialize()).ToList());

		return System.Text.Json.JsonSerializer.Serialize(serializedProject);
	}

	public static Project Deserialize(string serialized)
	{
		var serializedProject = System.Text.Json.JsonSerializer.Deserialize<SerializedProject>(serialized) ?? throw new Exception("Unable to deserialize project");

		var project = new Project(serializedProject.Id == default ? Guid.NewGuid() : serializedProject.Id);

		var nodeClasses = new Dictionary<NodeClass, NodeClass.SerializedNodeClass>();
		foreach (var nodeClassStr in serializedProject.Classes)
		{
			var nodeClass = NodeClass.Deserialize(nodeClassStr, project, out var serializedNodeClass);
			project.Classes.Add(nodeClass);

			nodeClasses[nodeClass] = serializedNodeClass;
		}

		// will deserialize methods and properties but not any graphs
		foreach (var nodeClass in nodeClasses)
			nodeClass.Key.Deserialize_Step2(nodeClass.Value);

		// deserialize graphs
		foreach (var nodeClass in nodeClasses)
			nodeClass.Key.Deserialize_Step3();


		return project;
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
