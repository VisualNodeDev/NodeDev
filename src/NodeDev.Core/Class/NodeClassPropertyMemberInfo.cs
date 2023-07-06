using NodeDev.Core.Types;

namespace NodeDev.Core.Class;

public interface IMemberInfo
{
	public TypeBase DeclaringType { get; }

	public string Name { get; }

	public TypeBase MemberType { get; }

	public bool IsStatic { get; }
}
