using NodeDev.Core.Class;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core
{
	public class Project
	{
		private record class SerializedProject(Guid Id, List<string> Classes);

		internal readonly Guid Id;

		public List<Class.NodeClass> Classes { get; } = new();

		private Dictionary<NodeClass, NodeClassType> NodeClassTypes = new();

		public Project(Guid id)
		{
			Id = id;
		}

		public static Project CreateNewDefaultProject()
		{
			var project = new Project(Guid.NewGuid());

			var programClass = new Class.NodeClass("Program", "NewProject", project);

			var main = new Class.NodeClassMethod(programClass, "Main", TypeFactory.Get(typeof(void)), new Graph());
			main.Graph.AddNode(new EntryNode(main.Graph));
			main.Graph.AddNode(new ReturnNode(main.Graph));
			programClass.Methods.Add(main);

			project.Classes.Add(programClass);

			return project;
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

			foreach (var nodeClass in serializedProject.Classes)
				project.Classes.Add(Class.NodeClass.Deserialize(nodeClass, project));

			return project;
		}

		public NodeClassType GetNodeClassType(NodeClass nodeClass)
		{
			if (!NodeClassTypes.ContainsKey(nodeClass))
				return NodeClassTypes[nodeClass] = new(nodeClass);
			return NodeClassTypes[nodeClass];
		}
	}
}
