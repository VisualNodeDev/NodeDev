using NodeDev.Core.Nodes;
using System.Reflection;

namespace NodeDev.Core.Types;

public interface IMethodInfo
{
	public string Name { get; }

	public bool IsStatic { get; }

	public TypeBase DeclaringType { get; }

	public TypeBase ReturnType { get; }

	public IEnumerable<IMethodParameterInfo> GetParameters();

	public Node.AlternateOverload AlternateOverload() => new(ReturnType, GetParameters().ToList());

	/// <summary>
	/// Create MethodInfo for the current method.
	/// If this is called on a NodeClassMethod, this assumes the project has already generated the classes types.
	/// </summary>
	/// <returns></returns>
	public MethodInfo CreateMethodInfo();

	public MethodAttributes Attributes { get; }
}

public interface IMethodParameterInfo
{
	public string Name { get; }

	public TypeBase ParameterType { get; }

	public bool IsOut { get; }

	public string FriendlyFormat()
	{
		return $"{(IsOut ? "out " : "")}{ParameterType.FriendlyName} {Name}";
	}
}
