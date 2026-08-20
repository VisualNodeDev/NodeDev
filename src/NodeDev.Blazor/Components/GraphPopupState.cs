using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Types;
using System.Numerics;

namespace NodeDev.Blazor.Components;

/// <summary>
/// Holds the transient state shared by GraphCanvas popup overlays. The show methods always reset the
/// previous state first, ensuring that node selection, overload selection, and generic type selection
/// remain mutually exclusive.
/// </summary>
internal sealed class GraphPopupState
{
	public bool IsShowingNodeSelection { get; private set; }
	public bool IsShowingGenericTypeSelection { get; private set; }
	public bool IsShowingOverloadSelection { get; private set; }

	public int X { get; private set; }
	public int Y { get; private set; }
	public Vector2 NodePosition { get; private set; }
	public Connection? NodeConnection { get; private set; }
	public Node? Node { get; private set; }
	public string? CallableScopeId { get; private set; }
	public string? GenericTypeName { get; private set; }
	public Action<TypeBase>? TypeSelectedAction { get; private set; }

	/// <summary>
	/// Opens node selection at the requested screen and graph positions, optionally carrying the connection
	/// and callable scope that the new node should be attached to.
	/// </summary>
	public void ShowNodeSelection(int x, int y, Vector2 nodePosition, Connection? connection, Node? node, string? callableScopeId)
	{
		Reset();
		X = x;
		Y = y;
		NodePosition = nodePosition;
		NodeConnection = connection;
		Node = node;
		CallableScopeId = callableScopeId;
		IsShowingNodeSelection = true;
	}

	/// <summary>
	/// Opens overload selection for a node whose callable target can be replaced by another overload.
	/// </summary>
	public void ShowOverloadSelection(Node node)
	{
		Reset();
		Node = node;
		IsShowingOverloadSelection = true;
	}

	/// <summary>
	/// Opens generic type selection for a node. A callback may override the canvas's default generic
	/// propagation behavior for specialized callers such as lambda group configuration.
	/// </summary>
	public void ShowGenericTypeSelection(int x, int y, Node node, string? genericTypeName, Action<TypeBase>? onTypeSelected = null)
	{
		Reset();
		X = x;
		Y = y;
		Node = node;
		GenericTypeName = genericTypeName;
		TypeSelectedAction = onTypeSelected;
		IsShowingGenericTypeSelection = true;
	}

	/// <summary>
	/// Closes every popup and releases all references associated with the previous selection operation.
	/// </summary>
	public void Reset()
	{
		IsShowingNodeSelection = false;
		IsShowingGenericTypeSelection = false;
		IsShowingOverloadSelection = false;
		NodeConnection = null;
		Node = null;
		CallableScopeId = null;
		GenericTypeName = null;
		TypeSelectedAction = null;
	}
}
