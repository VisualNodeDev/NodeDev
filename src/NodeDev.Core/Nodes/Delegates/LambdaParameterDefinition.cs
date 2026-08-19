using NodeDev.Core.Types;

namespace NodeDev.Core.Nodes.Delegates;

public sealed class LambdaParameterDefinition
{
	public LambdaParameterDefinition(string name, TypeBase type, string? id = null)
	{
		Id = id ?? Guid.NewGuid().ToString();
		Name = name;
		Type = type;
	}

	public string Id { get; }
	public string Name { get; internal set; }
	public TypeBase Type { get; internal set; }
}
