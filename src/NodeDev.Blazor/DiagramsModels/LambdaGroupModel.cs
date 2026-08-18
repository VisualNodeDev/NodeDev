using Blazor.Diagrams.Core.Models;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Delegates;

namespace NodeDev.Blazor.DiagramsModels;

/// <summary>
/// UI projection of a delegate-creation node and its callable body. Scope membership
/// in the core graph remains the source of truth; Children is only the live canvas view.
/// </summary>
public sealed class LambdaGroupModel : GroupModel
{
	public const byte MinimumPadding = 60;

	public LambdaGroupModel(CreateDelegateNode delegateNode, IEnumerable<NodeModel>? children = null)
		: base(children ?? [], CalculatePadding(delegateNode))
	{
		DelegateNode = delegateNode;
	}

	public CreateDelegateNode DelegateNode { get; }

	public GraphPortModel GetPort(Connection connection) =>
		Ports.OfType<GraphPortModel>().First(x => x.Connection == connection);

	public IEnumerable<GraphNodeModel> GetDescendantNodeModels()
	{
		foreach (var child in Children)
		{
			if (child is GraphNodeModel node)
			{
				yield return node;
			}
			else if (child is LambdaGroupModel group)
			{
				foreach (var descendant in group.GetDescendantNodeModels())
					yield return descendant;
			}
		}
	}

	private static byte CalculatePadding(CreateDelegateNode node)
	{
		return (byte)Math.Min(byte.MaxValue, MinimumPadding + node.CaptureInputs.Count * 15);
	}
}
