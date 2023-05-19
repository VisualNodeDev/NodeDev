using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core
{
	public class Project
	{
		private record class SerializedProject(List<string> Classes);

		public List<Class.NodeClass> Classes { get; } = new();

		public static Project CreateNewDefaultProject()
		{
			var project = new Project();

			var programClass = new Class.NodeClass("Program", "NewProject");

			var main = new Class.NodeClassMethod(programClass, "Main", TypeFactory.Get(typeof(void)), new Graph());
			main.Graph.AddNode(new EntryNode(main.Graph));
			main.Graph.AddNode(new ReturnNode(main.Graph));
			programClass.Methods.Add(main);

			project.Classes.Add(programClass);

			return project;
		}

		public string Serialize()
		{
			var serializedProject = new SerializedProject(Classes.Select(x => x.Serialize()).ToList());

			return System.Text.Json.JsonSerializer.Serialize(serializedProject);
		}

		public static Project Deserialize(string serialized)
		{
			var serializedProject = System.Text.Json.JsonSerializer.Deserialize<SerializedProject>(serialized) ?? throw new Exception("Unable to deserialize project");

			var project = new Project();

			foreach (var nodeClass in serializedProject.Classes)
				project.Classes.Add(Class.NodeClass.Deserialize(nodeClass));

			return project;
		}
	}
}
