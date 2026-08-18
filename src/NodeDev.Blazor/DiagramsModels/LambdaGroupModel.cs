using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using NodeDev.Blazor.NodeAttributes;
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
	public const byte FuncPadding = 90;
	public const double MinimumWidth = 600;
	public const double MinimumHeight = 420;

	public LambdaGroupModel(CreateDelegateNode delegateNode, IEnumerable<NodeModel>? children = null)
		: base(CreateChildren(delegateNode, children), CalculatePadding(delegateNode))
	{
		DelegateNode = delegateNode;
	}

	public CreateDelegateNode DelegateNode { get; }
	public LambdaReturnNode? BoundaryReturn => DelegateNode.Graph
		.GetNodesInScope(DelegateNode.BodyScopeId)
		.OfType<LambdaReturnNode>()
		.SingleOrDefault(x => x.IsImplicit);

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
		var minimum = node.Kind == DelegateKind.Func ? FuncPadding : MinimumPadding;
		return (byte)Math.Min(byte.MaxValue, minimum + node.CaptureInputs.Count * 15);
	}

	private static Point GetInitialPosition(CreateDelegateNode node, byte padding)
	{
		var childPositions = node.Graph.GetNodesInScope(node.BodyScopeId)
			.Where(x => x is not LambdaReturnNode { IsImplicit: true })
			.Where(x => x.Decorations.TryGetValue(typeof(NodeDecorationPosition), out _))
			.Select(x => (NodeDecorationPosition)x.Decorations[typeof(NodeDecorationPosition)])
			.ToList();
		if (childPositions.Count != 0)
		{
			return new Point(
				childPositions.Min(x => x.X) - padding,
				childPositions.Min(x => x.Y) - padding);
		}

		var position = node.GetOrAddDecoration<NodeDecorationPosition>(() => new(System.Numerics.Vector2.Zero));
		return new Point(position.X, position.Y);
	}

	private static IEnumerable<NodeModel> CreateChildren(CreateDelegateNode node, IEnumerable<NodeModel>? children)
	{
		var padding = CalculatePadding(node);
		var position = GetInitialPosition(node, padding);
		var layoutSize = new Size(
			Math.Max(1, MinimumWidth - padding * 2),
			Math.Max(1, MinimumHeight - padding * 2));
		yield return new LambdaLayoutModel(
			new Point(position.X + padding, position.Y + padding),
			layoutSize);

		foreach (var child in children ?? [])
			yield return child;
	}

	/// <summary>
	/// An invisible child that gives the auto-sized group a stable minimum workspace.
	/// Real children can still grow the group when they move outside these bounds.
	/// </summary>
	private sealed class LambdaLayoutModel : NodeModel
	{
		public LambdaLayoutModel(Point position, Size size) : base(position)
		{
			Size = size;
			ControlledSize = true;
			Locked = true;
			Visible = false;
		}
	}
}
