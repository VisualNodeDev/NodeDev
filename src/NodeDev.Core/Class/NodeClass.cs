using NodeDev.Core.Types;

namespace NodeDev.Core.Class
{
	public class NodeClass
	{
		public readonly Project Project;

		public TypeFactory TypeFactory => Project.TypeFactory;

		public TypeBase ClassTypeBase => Project.GetNodeClassType(this);

		public string Name { get; set; }

		public string Namespace { get; set; }

		public List<NodeClassMethod> Methods { get; } = new();

		public List<NodeClassProperty> Properties { get; } = new();

		public NodeClass(string name, string @namespace, Project project)
		{
			Name = name;
			Namespace = @namespace;
			Project = project;
		}

		#region Serialisation

		internal record class SerializedNodeClass(string Name, string Namespace, List<NodeClassMethod.SerializedNodeClassMethod> Methods, List<NodeClassProperty.SerializedNodeClassProperty> Properties);
		internal static NodeClass Deserialize(SerializedNodeClass serializedNodeClass, Project project)
		{
			var nodeClass = new NodeClass(serializedNodeClass.Name, serializedNodeClass.Namespace, project);

			return nodeClass;
		}

		internal void Deserialize_Step2(SerializedNodeClass serializedNodeClass)
		{
			foreach (var property in serializedNodeClass.Properties ?? [])
				Properties.Add(NodeClassProperty.Deserialize(this, property));

			foreach (var method in serializedNodeClass.Methods)
				Methods.Add(NodeClassMethod.Deserialize(this, method));
		}

		internal void Deserialize_Step3()
		{
			foreach (var method in Methods)
				method.Deserialize_Step3();
		}

		internal SerializedNodeClass Serialize()
		{
			var serializedNodeClass = new SerializedNodeClass(Name, Namespace, Methods.Select(x => x.Serialize()).ToList(), Properties.Select(x => x.Serialize()).ToList());

			return serializedNodeClass;
		}

		#endregion
	}
}
