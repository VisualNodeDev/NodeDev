using NodeDev.Core.Class;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NodeDev.Tests")]

namespace NodeDev.Core;

public class Project
{
	private record class SerializedProject(Guid Id, List<string> Classes);

	internal readonly Guid Id;

	public List<Class.NodeClass> Classes { get; } = new();

	private Dictionary<NodeClass, NodeClassType> NodeClassTypes = new();

	public readonly TypeFactory TypeFactory;

	public NodeClassTypeCreator? NodeClassTypeCreator { get; private set; }

	internal Subject<Graph> GraphChangedSubject { get; } = new();

	public IObservable<Graph> GraphChanged => GraphChangedSubject.AsObservable();

	public Project(Guid id)
	{
		Id = id;
		TypeFactory = new TypeFactory(this);
	}

	public IEnumerable<T> GetNodes<T>() where T : Node
	{
		return Classes.SelectMany(x => x.Methods).SelectMany(x => x.Graph.Nodes.Values).OfType<T>();
	}

	public static Project CreateNewDefaultProject()
	{
		var project = new Project(Guid.NewGuid());

		var programClass = new Class.NodeClass("Program", "NewProject", project);

		var main = new Class.NodeClassMethod(programClass, "Main", project.TypeFactory.Get(typeof(void)), new Graph());

		main.Graph.AddNode(new EntryNode(main.Graph));
		main.Graph.AddNode(new ReturnNode(main.Graph));
		programClass.Methods.Add(main);

		project.Classes.Add(programClass);

		return project;
	}

	public object? Run(object?[] inputs)
	{
		NodeClassTypeCreator = new();
		NodeClassTypeCreator.CreateProjectClassesAndAssembly(this);

		var program = Classes.Single(x => x.Name == "Program");

		var main = program.Methods.Single(x => x.Name == "Main");
		var executor = new GraphExecutor(main.Graph, null);

		var outputs = new object[main.ReturnType == TypeFactory.Get(typeof(void)) ? 1 : 2]; // 1 for the exec, 2 for exec + the actual return value
		executor.Execute(null, inputs, outputs);

		NodeClassTypeCreator = null;
		GC.Collect();

		return outputs[^1];
	}

	public Type GetCreatedClassType(NodeClass nodeClass)
	{
		if (NodeClassTypeCreator == null)
			throw new Exception("NodeClassTypeCreator is null, is the project currently being run?");

		if (NodeClassTypeCreator.GeneratedTypes.TryGetValue(GetNodeClassType(nodeClass), out var type))
			return type;

		throw new Exception("Unable to get generated node class for provided class: " + nodeClass.Name);
	}

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

	public NodeClassType GetNodeClassType(NodeClass nodeClass, TypeBase[]? generics = null)
	{
		if (!NodeClassTypes.ContainsKey(nodeClass))
			return NodeClassTypes[nodeClass] = new(nodeClass, generics ?? Array.Empty<TypeBase>());
		return NodeClassTypes[nodeClass];
	}
}
