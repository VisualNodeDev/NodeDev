using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;

namespace NodeDev.Core.Class
{
	public class NodeClass(string name, string @namespace, Project project)
	{
		public readonly Project Project = project;

		public TypeFactory TypeFactory => Project.TypeFactory;

		public TypeBase ClassTypeBase => Project.GetNodeClassType(this);

		public string Name { get; private set; } = name;

		public string Namespace { get; set; } = @namespace;

		internal List<NodeClassMethod> _Methods = [];
		public IReadOnlyList<NodeClassMethod> Methods => _Methods;

		public List<NodeClassProperty> Properties { get; } = [];

		#region AddMethod

		public void AddMethod(NodeClassMethod nodeClassMethod, bool createEntryAndReturn)
		{
			ArgumentNullException.ThrowIfNull(nodeClassMethod);
			if (nodeClassMethod.Class != this)
				throw new ArgumentException("The method belongs to a different class.", nameof(nodeClassMethod));
			if (_Methods.Any(method => method.Name == nodeClassMethod.Name && method.Parameters.Select(parameter => parameter.ParameterType).SequenceEqual(nodeClassMethod.Parameters.Select(parameter => parameter.ParameterType))))
				throw new InvalidOperationException($"A method named '{nodeClassMethod.Name}' with the same signature already exists.");

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

		public void RemoveMethod(NodeClassMethod method)
		{
			ArgumentNullException.ThrowIfNull(method);
			if (!_Methods.Contains(method))
				throw new InvalidOperationException("The method does not belong to this class.");

			var referencingCall = Project.GetNodes<Nodes.MethodCall>().FirstOrDefault(call => call.TargetMethod == method);
			if (referencingCall != null)
				throw new InvalidOperationException($"Method '{method.Name}' is still used by node '{referencingCall.Name}'. Remove those calls first.");

			_Methods.Remove(method);
		}

		public void Rename(string newName)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(newName);
			if (Project.Classes.Any(nodeClass => nodeClass != this && nodeClass.Namespace == Namespace && nodeClass.Name == newName))
				throw new InvalidOperationException($"A class named '{Namespace}.{newName}' already exists.");

			Name = newName;
			foreach (var method in Project.Classes.SelectMany(nodeClass => nodeClass.Methods))
				method.Graph.RaiseGraphChanged(true);
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
