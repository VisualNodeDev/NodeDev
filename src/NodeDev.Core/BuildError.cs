using NodeDev.Core.Nodes;

namespace NodeDev.Core;

public class BuildError : Exception
{
    public readonly Node Node;

    public BuildError(string message, Node node, Exception? inner) : base(message, inner)
    {
        Node = node;
    }
}
