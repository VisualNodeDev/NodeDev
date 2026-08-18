using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Delegates;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;

namespace NodeDev.Core.ManagerServices;

/// <summary>
/// Contains the logic on how to manipulate Graphs and Nodes.
/// </summary>
public class GraphManagerService
{
	private readonly IGraphCanvas GraphCanvas;

	private Graph Graph => GraphCanvas.Graph;

	internal GraphManagerService(IGraphCanvas graphCanvas)
	{
		GraphCanvas = graphCanvas;
	}

	#region Nodes

	/// <summary>
	/// Add the node from the search result.
	/// The <paramref name="populateNode"/> is used to add required UI information before adding it to the UI
	/// </summary>
	/// <param name="searchResult"></param>
	/// <param name="populateNode"></param>
	/// <returns></returns>
	public Node AddNode(NodeProvider.NodeSearchResult searchResult, Action<Node> populateNode)
		=> AddNode(searchResult, populateNode, callableScopeId: null);

	public Node AddNode(NodeProvider.NodeSearchResult searchResult, Action<Node> populateNode, string? callableScopeId)
	{
		var node = (Node)Activator.CreateInstance(searchResult.Type, [Graph, null])!;
		node.CallableScopeId = callableScopeId;

		if (searchResult is NodeProvider.DelegateCreationNode delegateCreation && node is CreateDelegateNode createDelegate)
			createDelegate.InitializeFromDelegateType(delegateCreation.DelegateType);
		else if (searchResult is NodeProvider.DelegateInvocationNode delegateInvocation && node is InvokeDelegateNode invokeDelegate)
			invokeDelegate.InitializeFromDelegateType(delegateInvocation.DelegateType);

		ValidateNodePlacement(node);
		populateNode(node);

		// add it to the nodes and the UI
		AddNode(node);

		if (searchResult is NodeProvider.MethodCallNode methodCall && node is MethodCall methodCallNode)
			methodCallNode.SetMethodTarget(methodCall.MethodInfo);
		else if (searchResult is NodeProvider.GetPropertyOrFieldNode getPropertyOrField && node is GetPropertyOrField getPropertyOrFieldNode)
			getPropertyOrFieldNode.SetMemberTarget(getPropertyOrField.MemberInfo);
		else if (searchResult is NodeProvider.SetPropertyOrFieldNode setPropertyOrField && node is SetPropertyOrField setPropertyOrFieldNode)
			setPropertyOrFieldNode.SetMemberTarget(setPropertyOrField.MemberInfo);

		if (node is CreateDelegateNode delegateNode)
			CreateDefaultDelegateBody(delegateNode);

		return node;
	}

	public void AddNode(Node node, string? callableScopeId)
	{
		node.CallableScopeId = callableScopeId;
		ValidateNodePlacement(node);
		AddNode(node);
	}

	public void AddDelegateNode(CreateDelegateNode node, string? callableScopeId)
	{
		AddNode(node, callableScopeId);
		CreateDefaultDelegateBody(node);
	}

	public void AddNode(Node node)
	{
		ValidateNodePlacement(node);
		((IDictionary<string, Node>)Graph.Nodes)[node.Id] = node;

		GraphCanvas.AddNode(node);
	}


	public void RemoveNode(Node node)
	{
		var removalOrder = GetRecursiveRemovalOrder(node);
		var disconnectedPairs = new HashSet<(string, string)>();

		foreach (var removedNode in removalOrder)
		{
			foreach (var connection in removedNode.InputsAndOutputs)
			{
				foreach (var other in connection.Connections.ToList())
				{
					var pair = string.CompareOrdinal(connection.Id, other.Id) < 0 ? (connection.Id, other.Id) : (other.Id, connection.Id);
					if (disconnectedPairs.Add(pair))
						DisconnectConnectionBetween(connection, other);
				}
			}
		}

		foreach (var removedNode in removalOrder)
		{
			Graph._Nodes.Remove(removedNode.Id);
			GraphCanvas.RemoveNode(removedNode);
		}

		Graph.RaiseGraphChanged(false);
	}

	private List<Node> GetRecursiveRemovalOrder(Node root)
	{
		var result = new List<Node>();
		var visited = new HashSet<Node>();

		void Visit(Node node)
		{
			if (!visited.Add(node))
				return;
			if (node is CreateDelegateNode owner)
			{
				foreach (var child in Graph.GetNodesInScope(owner.BodyScopeId).ToList())
					Visit(child);
			}
			result.Add(node);
		}

		Visit(root);
		return result;
	}

	private void CreateDefaultDelegateBody(CreateDelegateNode owner)
	{
		var entry = new LambdaEntryNode(Graph) { CallableScopeId = owner.BodyScopeId };
		Node terminal = owner.Kind == DelegateKind.Func
			? new LambdaReturnNode(Graph) { CallableScopeId = owner.BodyScopeId }
			: new LambdaCompleteNode(Graph) { CallableScopeId = owner.BodyScopeId };

		entry.RefreshFromOwner(owner);
		if (terminal is LambdaReturnNode lambdaReturn)
			lambdaReturn.RefreshFromOwner(owner);

		AddNode(entry);
		AddNode(terminal);
		AddNewConnectionBetween(entry.ExecOutput, terminal.Inputs[0]);
	}

	private void ValidateNodePlacement(Node node)
	{
		if ((node is EntryNode || node is ReturnNode) && node.CallableScopeId != null)
			throw new InvalidOperationException("Method entry and return nodes can only be added to the root method scope.");
		if (node is LambdaEntryNode or LambdaReturnNode or LambdaCompleteNode)
		{
			var owner = Graph.GetOwningLambda(node.CallableScopeId)
				?? throw new InvalidOperationException("Lambda entry and terminal nodes require a valid owning lambda scope.");
			if (node is LambdaReturnNode && owner.Kind != DelegateKind.Func)
				throw new InvalidOperationException("Lambda return nodes can only be added to Func scopes.");
			if (node is LambdaCompleteNode && owner.Kind != DelegateKind.Action)
				throw new InvalidOperationException("Lambda completion nodes can only be added to Action scopes.");
		}
		else if (node.CallableScopeId != null && Graph.GetOwningLambda(node.CallableScopeId) == null)
			throw new InvalidOperationException($"Cannot add a node to orphaned callable scope '{node.CallableScopeId}'.");
	}

	#endregion

	#region Connections

	/// <summary>
	/// Connects ports normally when they share a scope. When a data value flows from
	/// an enclosing scope into a lambda, creates and wires the required captures.
	/// </summary>
	public void AddNewConnectionBetweenOrCapture(Connection source, Connection destination)
	{
		if (source.IsInput)
			(destination, source) = (source, destination);

		if (source.Parent.CallableScopeId == destination.Parent.CallableScopeId)
		{
			AddNewConnectionBetween(source, destination);
			return;
		}

		if (!source.IsOutput || !destination.IsInput)
			throw new InvalidOperationException("Graph connections must connect an output port to an input port.");
		if (source.Parent.Graph != Graph || destination.Parent.Graph != Graph)
			throw new InvalidOperationException("Cannot connect ports from another graph.");
		if (source.Type.IsExec || destination.Type.IsExec)
			throw new InvalidOperationException("Execution flow cannot be captured across a callable scope boundary.");
		if (!source.IsAssignableTo(destination, true, true, out _, out _, out _))
			throw new InvalidOperationException($"Cannot assign '{source.Type.FriendlyName}' to '{destination.Type.FriendlyName}'.");

		var owners = GetAutomaticCaptureRoute(source.Parent.CallableScopeId, destination.Parent.CallableScopeId);
		var captureName = GetAutomaticCaptureName(source);
		var scopedSource = source;

		foreach (var owner in owners)
		{
			owner.AddCapture(captureName, scopedSource.Type);
			GraphCanvas.Refresh(owner);

			var captureIndex = owner.Captures.Count - 1;
			var entry = Graph.GetNodesInScope(owner.BodyScopeId).OfType<LambdaEntryNode>().Single();
			AddNewConnectionBetween(scopedSource, owner.CaptureInputs[captureIndex]);
			scopedSource = entry.CaptureOutputs[captureIndex];
		}

		AddNewConnectionBetween(scopedSource, destination);
	}

	private List<CreateDelegateNode> GetAutomaticCaptureRoute(string? sourceScopeId, string? destinationScopeId)
	{
		var owners = new List<CreateDelegateNode>();
		var currentScopeId = destinationScopeId;

		while (currentScopeId != sourceScopeId)
		{
			var owner = Graph.GetOwningLambda(currentScopeId)
				?? throw new InvalidOperationException("Automatic captures can only connect a value from an enclosing scope into a nested lambda scope.");
			owners.Add(owner);
			currentScopeId = owner.CallableScopeId;
		}

		owners.Reverse();
		return owners;
	}

	private static string GetAutomaticCaptureName(Connection source)
	{
		const string capturedPrefix = "Captured ";
		var name = source.Name;
		if (name.StartsWith(capturedPrefix, StringComparison.OrdinalIgnoreCase))
			name = name[capturedPrefix.Length..];
		return string.IsNullOrWhiteSpace(name) ? "capture" : name;
	}

	public void MergeRemovedConnectionsWithNewConnections(IEnumerable<Connection> newConnections, IEnumerable<Connection> removedConnections)
	{
		foreach (var removedConnection in removedConnections)
		{
			var newConnection = newConnections.FirstOrDefault(x => x.Parent == removedConnection.Parent && x.Name == removedConnection.Name && x.Type == removedConnection.Type);

			// if we found a new connection, connect them together and remove the old connection
			foreach (var oldLink in removedConnection.Connections.ToList())
			{
				DisconnectConnectionBetween(oldLink, removedConnection); // cleanup the old connection

				if (newConnection != null)
				{
					// Before we re-connect them let's make sure both are inputs or both outputs
					if (oldLink.IsInput != newConnection.IsInput)
					{
						// we can safely reconnect the new connection to the old link
						// Either newConnection is an input and removedConnection is an output or vice versa
						AddNewConnectionBetween(oldLink, newConnection);
					}
				}
			}
		}
	}

	public void AddNewConnectionBetween(Connection source, Connection destination)
	{
		if (source.IsInput)
		{
			(destination, source) = (source, destination);
		}

		if (!source.IsOutput || !destination.IsInput)
			throw new InvalidOperationException("Graph connections must connect an output port to an input port.");
		if (source.Parent.Graph != Graph || destination.Parent.Graph != Graph)
			throw new InvalidOperationException("Cannot connect ports from another graph.");
		if (source.Parent.CallableScopeId != destination.Parent.CallableScopeId)
			throw new InvalidOperationException($"Cannot connect '{source.Parent.Name}.{source.Name}' to '{destination.Parent.Name}.{destination.Name}' across a callable scope boundary. Add an explicit lambda capture instead.");

		if (!source._Connections.Contains(destination))
			source._Connections.Add(destination);
		if (!destination._Connections.Contains(source))
			destination._Connections.Add(source);

		// we're plugging something something with a generic into something without a generic
		if (source.IsAssignableTo(destination, true, true, out var newTypesLeft, out var newTypesRight, out var usedInitialTypes))
		{
			if (newTypesLeft.Count != 0)
				PropagateNewGeneric(source.Parent, newTypesLeft, usedInitialTypes, destination, false);
			if (newTypesRight.Count != 0)
				PropagateNewGeneric(destination.Parent, newTypesRight, usedInitialTypes, source, false);
		}

		GraphCanvas.AddLinkToGraphCanvas(source, destination);

		GraphCanvas.UpdatePortColor(source);
		GraphCanvas.UpdatePortColor(destination);

		// we have to disconnect the previously connected exec, since exec outputs can only have one connection
		if (source.Type.IsExec && source.Connections.Count > 1)
			DisconnectConnectionBetween(source, source.Connections.First(x => x != destination));
		else if (!destination.Type.IsExec && destination.Connections.Count > 1) // non-exec inputs can only have one connection
			DisconnectConnectionBetween(destination.Connections.First(x => x != source), destination);

		Graph.RaiseGraphChanged(false); // any change in the graph should trigger a UI refresh already, lets just trigger at least one non-ui refresh to be sure
	}

	public void DisconnectConnectionBetween(Connection source, Connection destination)
	{
		if (source.IsInput)
		{
			(destination, source) = (source, destination);
		}

		source._Connections.Remove(destination);
		destination._Connections.Remove(source);

		GraphCanvas.RemoveLinkFromGraphCanvas(source, destination);

		GraphCanvas.UpdatePortColor(source);
		GraphCanvas.UpdatePortColor(destination);

		Graph.RaiseGraphChanged(false); // no ui refresh needed as we already took care of it through the GraphCanvas directly
	}

	/// <summary>
	/// Propagate the new generic type to all the connections of the node and recursively to the connected nodes.
	/// </summary>
	/// <param name="initiatingConnection">The connection that initiated the propagation. This is used to avoid reupdating back and forth, sometimes erasing information in the process.</param>
	public void PropagateNewGeneric(Node node, IReadOnlyDictionary<string, TypeBase> changedGenerics, bool useInitialTypes, Connection? initiatingConnection, bool overrideInitialTypes)
	{
		node.OnBeforeGenericTypeDefined(changedGenerics);

		bool hadAnyChanges = false;
		foreach (var port in node.InputsAndOutputs.ToList()) // check if any of the ports have the generic we just solved
		{
			var previousType = useInitialTypes ? port.InitialType : port.Type;

			if (!previousType.GetUndefinedGenericTypes().Any(changedGenerics.ContainsKey))
				continue;

			// update port.Type property as well as the textbox visibility if necessary
			port.UpdateTypeAndTextboxVisibility(previousType.ReplaceUndefinedGeneric(changedGenerics), overrideInitialType: overrideInitialTypes);
			hadAnyChanges |= node.GenericConnectionTypeDefined(port).Count != 0;
			GraphCanvas.UpdatePortColor(port);

			var isPortInput = port.IsInput; // cache for performance, IsInput is slow
											// check if other connections had their own generics and if we just solved them
			foreach (var other in port.Connections.ToList()) // ToList is required since we're modifying the list in the loop
			{
				if (other == initiatingConnection)
					continue;

				var source = isPortInput ? other : port;
				var target = isPortInput ? port : other;
				if (source.IsAssignableTo(target, isPortInput, !isPortInput, out var changedGenericsLeft2, out var changedGenericsRight2, out var usedInitialTypes) && (changedGenericsLeft2.Count != 0 || changedGenericsRight2.Count != 0))
				{
					if (changedGenericsLeft2.Count != 0)
						PropagateNewGeneric(port.Parent, changedGenericsLeft2, usedInitialTypes, other, false);
					if (changedGenericsRight2.Count != 0)
						PropagateNewGeneric(other.Parent, changedGenericsRight2, usedInitialTypes, port, false);
				}
				else if ((changedGenericsLeft2?.Count ?? 0) != 0)// looks like changing the generic made it so we can't link to this connection anymore
					DisconnectConnectionBetween(port, other);
			}
		}

		if (hadAnyChanges)
			Graph.RaiseGraphChanged(false);
	}

	public void SelectNodeOverload(Node popupNode, Node.AlternateOverload overload)
	{
		popupNode.SelectOverload(overload, out var newConnections, out var removedConnections);

		MergeRemovedConnectionsWithNewConnections(newConnections, removedConnections);
	}

	#endregion
}
