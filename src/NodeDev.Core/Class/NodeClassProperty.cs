using NodeDev.Core.Types;

namespace NodeDev.Core.Class;

public class NodeClassProperty : IMemberInfo
{
	internal record class SerializedNodeClassProperty(string Name, TypeBase.SerializedType Type);
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

	public bool IsField => false;

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
				var hasAnyGetProperty = method.Graph.Nodes.Values.OfType<Nodes.GetPropertyOrField>().Any(x => x.TargetMember == this);
				var hasAnySetProperty = method.Graph.Nodes.Values.OfType<Nodes.SetPropertyOrField>().Any(x => x.TargetMember == this);

				if (hasAnySetProperty || hasAnyGetProperty)
					method.Graph.RaiseGraphChanged(true);
			}
		}
	}

	#endregion

	#region Serialization

	internal static NodeClassProperty Deserialize(NodeClass owner, SerializedNodeClassProperty serializedNodeClassProperty)
	{
		var returnType = TypeBase.Deserialize(owner.Project.TypeFactory, serializedNodeClassProperty.Type);
		var nodeClassProperty = new NodeClassProperty(owner, serializedNodeClassProperty.Name, returnType);

		return nodeClassProperty;
	}

	internal SerializedNodeClassProperty Serialize()
	{
		var serializedNodeClassProperty = new SerializedNodeClassProperty(Name, PropertyType.SerializeWithFullTypeName());

		return serializedNodeClassProperty;
	}

	#endregion
}
