using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Class
{
	public class NodeClassProperty
	{
		private record class SerializedNodeClassProperty(string Name, string TypeFullName, string Type);
		public NodeClassProperty(NodeClass ownerClass, string name, TypeBase propertyType)
		{
			Class = ownerClass;
			Name = name;
			PropertyType = propertyType;
		}


		public NodeClass Class { get; }

		public string Name { get; private set; }

		public TypeBase PropertyType { get; }

		public List<NodeClassMethodParameter> Parameters { get; } = new();

		public void Rename(string newName)
		{
			if (string.IsNullOrWhiteSpace(newName))
				return;

			Name = newName;
		}

		#region Serialization

		public static NodeClassProperty Deserialize(NodeClass owner, string serialized)
		{
			var serializedNodeClassProperty = System.Text.Json.JsonSerializer.Deserialize<SerializedNodeClassProperty>(serialized) ?? throw new Exception("Unable to deserialize node class property");

			var returnType = TypeBase.Deserialize(owner.Project.TypeFactory, serializedNodeClassProperty.TypeFullName, serializedNodeClassProperty.Type);
			var nodeClassProperty = new NodeClassProperty(owner, serializedNodeClassProperty.Name, returnType);

			return nodeClassProperty;
		}

		public string Serialize()
		{
			var serializedNodeClassProperty = new SerializedNodeClassProperty(Name, PropertyType.GetType().FullName!, PropertyType.FullName);
			return System.Text.Json.JsonSerializer.Serialize(serializedNodeClassProperty);
		}

		#endregion
	}
}
