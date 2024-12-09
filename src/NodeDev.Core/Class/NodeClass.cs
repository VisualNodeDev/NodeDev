using NodeDev.Core.ManagerServices;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;

namespace NodeDev.Core.Class
{
	public class NodeClass(string name, string @namespace, Project project)
	{
		public readonly Project Project = project;

		public TypeFactory TypeFactory => Project.TypeFactory;

		public TypeBase ClassTypeBase => Project.GetNodeClassType(this);

		public string Name { get; set; } = name;

		public string Namespace { get; set; } = @namespace;

		internal List<NodeClassMethod> _Methods = [];
		public IReadOnlyList<NodeClassMethod> Methods => _Methods;

		public List<NodeClassProperty> Properties { get; } = [];

		#region AddMethod

		public void AddMethod(NodeClassMethod nodeClassMethod, bool createEntryAndReturn)
		{
			_Methods.Add(nodeClassMethod);

			if (!createEntryAndReturn)
				return;

			// Create entry and return node for the method
			var entry = new EntryNode(nodeClassMethod.Graph);
			var returnNode = new ReturnNode(nodeClassMethod.Graph);

			nodeClassMethod.Manager.AddNode(entry);
			nodeClassMethod.Manager.AddNode(returnNode);

			// Link the execution path
			nodeClassMethod.Manager.AddNewConnectionBetween(entry.Outputs[0], returnNode.Inputs[0]);
		}

		#endregion

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
				_Methods.Add(NodeClassMethod.Deserialize(this, method));
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
