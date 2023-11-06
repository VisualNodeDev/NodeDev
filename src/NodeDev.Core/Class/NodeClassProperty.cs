using NodeDev.Core.Types;

namespace NodeDev.Core.Class;

public class NodeClassProperty : IMemberInfo
{
	private record class SerializedNodeClassProperty(string Name, string Type);
	public NodeClassProperty(NodeClass ownerClass, string name, TypeBase propertyType)
	{
		Class = ownerClass;
		Name = name;
		PropertyType = propertyType;
	}

	public NodeClass Class { get; }

	public string Name { get; private set; }

	public TypeBase PropertyType { get; private set; }

	public List<NodeClassMethodParameter> Parameters { get; } = new();

	public TypeBase DeclaringType => Class.ClassTypeBase;

	public TypeBase MemberType => PropertyType;

	public bool IsStatic => false;

	public bool CanGet => true;

	public bool CanSet => true;

	#region UI Actions

	public void Rename(string newName)
	{
		if (string.IsNullOrWhiteSpace(newName))
			return;

		Name = newName;
		UpdateGraphUsingProperty();
	}

	public void ChangeType(TypeBase type)
	{
		PropertyType = type;

		UpdateGraphUsingProperty();
	}

	private void UpdateGraphUsingProperty()
	{
		foreach (var nodeClass in Class.Project.Classes)
		{
			foreach (var method in nodeClass.Methods)
			{
				var hasAnyGetProperty = method.Graph.Nodes.Values.OfType<Nodes.GetPropertyOrField>().Any();
				var hasAnySetProperty = method.Graph.Nodes.Values.OfType<Nodes.SetPropertyOrField>().Any();

				if (hasAnySetProperty || hasAnyGetProperty)
					Class.Project.GraphChangedSubject.OnNext(method.Graph);
			}
		}
	}

	#endregion

	#region Serialization

	public static NodeClassProperty Deserialize(NodeClass owner, string serialized)
	{
		var serializedNodeClassProperty = System.Text.Json.JsonSerializer.Deserialize<SerializedNodeClassProperty>(serialized) ?? throw new Exception("Unable to deserialize node class property");

		var returnType = TypeBase.Deserialize(owner.Project.TypeFactory, serializedNodeClassProperty.Type);
		var nodeClassProperty = new NodeClassProperty(owner, serializedNodeClassProperty.Name, returnType);

		return nodeClassProperty;
	}

	public string Serialize()
	{
		var serializedNodeClassProperty = new SerializedNodeClassProperty(Name, PropertyType.SerializeWithFullTypeName());
		return System.Text.Json.JsonSerializer.Serialize(serializedNodeClassProperty);
	}

	#endregion
}
