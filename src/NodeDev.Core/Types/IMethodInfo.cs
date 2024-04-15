using NodeDev.Core.Nodes;
using NodeDev.Core.Types;

namespace NodeDev.Core.Types;

public interface IMethodInfo
{
    public string Name { get; }

    public bool IsStatic { get; }

    public TypeBase DeclaringType { get; }

    public TypeBase ReturnType { get; }

    public IEnumerable<IMethodParameterInfo> GetParameters();

    public Node.AlternateOverload AlternateOverload() => new(ReturnType, GetParameters().ToList());
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
