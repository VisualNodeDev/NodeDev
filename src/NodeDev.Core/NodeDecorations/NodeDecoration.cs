using NodeDev.Core.Types;

namespace NodeDev.Core.NodeDecorations;

public interface INodeDecoration
{
	public static INodeDecoration Deserialize(TypeFactory typeFactory, string serialized) => throw new NotImplementedException();

	public abstract string Serialize();
}
