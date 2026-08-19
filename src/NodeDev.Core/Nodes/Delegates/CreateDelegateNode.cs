using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;
using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Text.Json;

namespace NodeDev.Core.Nodes.Delegates;

public abstract class CreateDelegateNode : NoFlowNode
{
	private readonly List<LambdaParameterDefinition> _parameters = [];
	private readonly List<LambdaCaptureDefinition> _captures = [];

	protected CreateDelegateNode(Graph graph, string? id = null) : base(graph, id)
	{
	}

	public abstract DelegateKind Kind { get; }
	public IReadOnlyList<LambdaParameterDefinition> Parameters => _parameters;
	public TypeBase? ResultType { get; private set; }
	public IReadOnlyList<LambdaCaptureDefinition> Captures => _captures;
	public string BodyScopeId => Id;
	public TypeBase DelegateType => BclDelegateType.Create(TypeFactory, Kind, _parameters.Select(x => x.Type).ToArray(), ResultType);
	public IReadOnlyList<Connection> CaptureInputs => Inputs;
	public Connection DelegateOutput => Outputs.Single();
	public string SignatureDisplayName => DelegateType.FriendlyName;

	protected void InitializeSignature(TypeBase? resultType)
	{
		ResultType = resultType;
		ReconcileOwnPorts();
	}

	public LambdaParameterDefinition AddParameter(string? name = null, TypeBase? type = null)
	{
		if (_parameters.Count >= BclDelegateType.MaximumParameterCount)
			throw new InvalidOperationException($"A delegate cannot have more than {BclDelegateType.MaximumParameterCount} parameters.");

		var definition = new LambdaParameterDefinition(GetUniqueDefinitionName(name, "value", _parameters.Select(x => x.Name)), type ?? TypeFactory.Get<int>());
		_parameters.Add(definition);
		RefreshSignatureProjection();
		return definition;
	}

	public void RemoveParameter(string parameterId)
	{
		var index = _parameters.FindIndex(x => x.Id == parameterId);
		if (index < 0)
			return;

		foreach (var entry in Graph.GetNodesInScope(BodyScopeId).OfType<LambdaEntryNode>())
			RemoveConnectionAt(entry.Outputs, index + 1);
		_parameters.RemoveAt(index);
		RefreshSignatureProjection();
	}

	public void UpdateParameter(string parameterId, string name, TypeBase type)
	{
		var parameter = _parameters.FirstOrDefault(x => x.Id == parameterId)
			?? throw new ArgumentException("Unknown lambda parameter.", nameof(parameterId));
		EnsureDefinitionNameAvailable(name, parameter.Id, _parameters.Select(x => (x.Id, x.Name)));
		parameter.Name = name;
		parameter.Type = type;
		RefreshSignatureProjection();
	}

	public LambdaCaptureDefinition AddCapture(string? name = null, TypeBase? type = null)
	{
		var definition = new LambdaCaptureDefinition(GetUniqueDefinitionName(name, "capture", _captures.Select(x => x.Name)), type ?? TypeFactory.Get<int>());
		_captures.Add(definition);
		RefreshSignatureProjection();
		return definition;
	}

	public void RemoveCapture(string captureId)
	{
		var index = _captures.FindIndex(x => x.Id == captureId);
		if (index < 0)
			return;

		RemoveConnectionAt(Inputs, index);
		foreach (var entry in Graph.GetNodesInScope(BodyScopeId).OfType<LambdaEntryNode>())
			RemoveConnectionAt(entry.Outputs, 1 + _parameters.Count + index);
		_captures.RemoveAt(index);
		RefreshSignatureProjection();
	}

	public void UpdateCapture(string captureId, string name, TypeBase type)
	{
		var capture = _captures.FirstOrDefault(x => x.Id == captureId)
			?? throw new ArgumentException("Unknown lambda capture.", nameof(captureId));
		EnsureDefinitionNameAvailable(name, capture.Id, _captures.Select(x => (x.Id, x.Name)));
		capture.Name = name;
		capture.Type = type;
		RefreshSignatureProjection();
	}

	public void SetResultType(TypeBase resultType)
	{
		if (Kind != DelegateKind.Func)
			throw new InvalidOperationException("Only Func delegates have a result type.");
		ResultType = resultType;
		RefreshSignatureProjection();
	}

	internal void InitializeFromDelegateType(TypeBase delegateType)
	{
		if (!BclDelegateType.TryDescribe(delegateType, out var kind, out var parameterTypes, out var resultType) || kind != Kind)
			throw new ArgumentException($"{delegateType.FriendlyName} is not a supported {Kind} delegate type.", nameof(delegateType));

		_parameters.Clear();
		for (var index = 0; index < parameterTypes.Count; index++)
			_parameters.Add(new LambdaParameterDefinition(GetUniqueDefinitionName(null, $"value{index + 1}", _parameters.Select(x => x.Name)), parameterTypes[index]));
		ResultType = resultType;
		RefreshSignatureProjection();
	}

	public override void OnBeforeGenericTypeDefined(IReadOnlyDictionary<string, TypeBase> changedGenerics)
	{
		foreach (var parameter in _parameters)
			parameter.Type = parameter.Type.ReplaceUndefinedGeneric(changedGenerics);
		foreach (var capture in _captures)
			capture.Type = capture.Type.ReplaceUndefinedGeneric(changedGenerics);
		if (ResultType != null)
			ResultType = ResultType.ReplaceUndefinedGeneric(changedGenerics);
	}

	public override List<Connection> GenericConnectionTypeDefined(Connection connection)
	{
		RefreshSignatureProjection();
		return InputsAndOutputs
			.Concat(Graph.GetNodesInScope(BodyScopeId).SelectMany(x => x.InputsAndOutputs))
			.Distinct()
			.ToList();
	}

	internal void RefreshSignatureProjection()
	{
		ReconcileOwnPorts();

		foreach (var entry in Graph.GetNodesInScope(BodyScopeId).OfType<LambdaEntryNode>())
			entry.RefreshFromOwner(this);
		foreach (var returnNode in Graph.GetNodesInScope(BodyScopeId).OfType<LambdaReturnNode>())
			returnNode.RefreshFromOwner(this);

		Graph.RaiseGraphChanged(true);
	}

	private void ReconcileOwnPorts()
	{
		ReconcileConnections(this, Inputs, _captures.Select(x => (x.Name, x.Type)));
		ReconcileConnections(this, Outputs, [("Delegate", DelegateType)]);
	}

	internal static void ReconcileConnections(Node parent, List<Connection> connections, IEnumerable<(string Name, TypeBase Type)> desiredConnections)
	{
		var desired = desiredConnections.ToList();

		while (connections.Count > desired.Count)
		{
			var removed = connections[^1];
			foreach (var other in removed.Connections.ToList())
				removed.Parent.Graph.Manager.DisconnectConnectionBetween(removed, other);
			connections.RemoveAt(connections.Count - 1);
		}

		for (var index = 0; index < desired.Count; index++)
		{
			var item = desired[index];
			if (index >= connections.Count)
			{
				connections.Add(new Connection(item.Name, parent, item.Type));
				continue;
			}

			connections[index].Name = item.Name;
			if (connections[index].Type != item.Type)
			{
				connections[index].UpdateTypeAndTextboxVisibility(item.Type, overrideInitialType: true);
				DisconnectIncompatibleLinks(connections[index]);
			}
		}
	}

	private static void RemoveConnectionAt(List<Connection> connections, int index)
	{
		if (index < 0 || index >= connections.Count)
			return;
		var removed = connections[index];
		foreach (var other in removed.Connections.ToList())
			removed.Parent.Graph.Manager.DisconnectConnectionBetween(removed, other);
		connections.RemoveAt(index);
	}

	private static void DisconnectIncompatibleLinks(Connection connection)
	{
		foreach (var other in connection.Connections.ToList())
		{
			var source = connection.IsOutput ? connection : other;
			var destination = connection.IsInput ? connection : other;
			if (!source.IsAssignableTo(destination, true, true, out _, out _, out _))
				connection.Parent.Graph.Manager.DisconnectConnectionBetween(connection, other);
		}
	}

	private static string GetUniqueDefinitionName(string? requestedName, string fallback, IEnumerable<string> existingNames)
	{
		var existing = existingNames.ToHashSet(StringComparer.Ordinal);
		var baseName = string.IsNullOrWhiteSpace(requestedName) ? fallback : requestedName.Trim();
		var name = baseName;
		var suffix = 2;
		while (existing.Contains(name))
			name = $"{baseName}_{suffix++}";
		return name;
	}

	private static void EnsureDefinitionNameAvailable(string name, string currentId, IEnumerable<(string Id, string Name)> definitions)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("Lambda definition names cannot be empty.", nameof(name));
		if (definitions.Any(x => x.Id != currentId && x.Name == name))
			throw new ArgumentException($"A lambda definition named '{name}' already exists.", nameof(name));
	}

	private sealed record SerializedDefinition(string Id, string Name, TypeBase.SerializedType Type);
	private sealed record SerializedDelegatePayload(DelegateKind Kind, List<SerializedDefinition> Parameters, TypeBase.SerializedType? ResultType, List<SerializedDefinition> Captures);

	protected override string? SerializePayload()
	{
		return JsonSerializer.Serialize(new SerializedDelegatePayload(
			Kind,
			_parameters.Select(x => new SerializedDefinition(x.Id, x.Name, x.Type.SerializeWithFullTypeName())).ToList(),
			ResultType?.SerializeWithFullTypeName(),
			_captures.Select(x => new SerializedDefinition(x.Id, x.Name, x.Type.SerializeWithFullTypeName())).ToList()));
	}

	protected override void DeserializePayload(string? payload)
	{
		if (payload == null)
			return;

		var serialized = JsonSerializer.Deserialize<SerializedDelegatePayload>(payload)
			?? throw new InvalidOperationException("Unable to deserialize delegate signature payload.");
		if (serialized.Kind != Kind)
			throw new InvalidOperationException($"Serialized delegate kind {serialized.Kind} does not match node type {Kind}.");

		_parameters.Clear();
		_parameters.AddRange(serialized.Parameters.Select(x => new LambdaParameterDefinition(x.Name, TypeBase.Deserialize(TypeFactory, x.Type), x.Id)));
		_captures.Clear();
		_captures.AddRange(serialized.Captures.Select(x => new LambdaCaptureDefinition(x.Name, TypeBase.Deserialize(TypeFactory, x.Type), x.Id)));
		ResultType = serialized.ResultType == null ? null : TypeBase.Deserialize(TypeFactory, serialized.ResultType);
	}

	internal override void FinalizeDeserialization()
	{
		ReconcileOwnPorts();
	}

	internal override ExpressionSyntax GenerateRoslynExpression(GenerationContext context)
	{
		return new RoslynGraphBuilder(Graph, context).BuildLambdaExpression(this);
	}
}
