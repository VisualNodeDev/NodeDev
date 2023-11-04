using NodeDev.Core.Types;

namespace NodeDev.Core.Types;

public interface IMethodInfo
{
	public string Name { get; }

	public bool IsStatic { get; }

	public TypeBase DeclaringType { get; }

	public TypeBase ReturnType { get; }

	public IEnumerable<IMethodParameterInfo> GetParameters();
}

public interface IMethodParameterInfo
{
	public string Name { get; }

	public TypeBase ParameterType { get; }
}
