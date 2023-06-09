using NodeDev.Core.Types;

namespace NodeDev.Core.Class;

public interface IMemberInfo
{
	public TypeBase DeclaringType { get; }

	public string Name { get; }

	public TypeBase MemberType { get; }

	public bool IsStatic { get; }
}

internal class NodeClassPropertyMemberInfo : IMemberInfo
{
	private NodeClassProperty Property;

	public NodeClassPropertyMemberInfo(NodeClassProperty property)
	{
		Property = property;
	}

	public TypeBase DeclaringType => Property.Class.TypeFactory.Get(Property.Class);

	public string Name => Property.Name;

	public TypeBase MemberType => Property.PropertyType;

	public bool IsStatic => false;
}
