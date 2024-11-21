using NodeDev.Core.NodeDecorations;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes;

/// <summary>
/// Returns the selected type. Represent C# '<see langword="typeof"/>' keyword.
/// </summary>
public class TypeOf : NoFlowNode
{
	private class CurrentUndefinedType : INodeDecoration
	{
		public TypeBase Type { get; }

		public CurrentUndefinedType(TypeBase undefinedType)
		{
			Type = undefinedType;
		}

		public string Serialize()
		{
			return Type.SerializeWithFullTypeNameString();
		}

		public static INodeDecoration Deserialize(TypeFactory typeFactory, string json)
		{
			return new CurrentUndefinedType(TypeBase.DeserializeFullTypeNameString(typeFactory, json));
		}
	}

	public TypeOf(Graph graph, string? id = null) : base(graph, id)
	{
		Outputs.Add(new("Type", this, TypeFactory.Get<Type>()));

		Type = new UndefinedGenericType("T");
		Decorations.Add(typeof(CurrentUndefinedType), new CurrentUndefinedType(Type));
	}

	private TypeBase Type;

	public override string Name => "TypeOf";

	public override IEnumerable<string> GetUndefinedGenericTypes()
	{
		if (Type is UndefinedGenericType undefined)
			return [undefined.Name];

		return [];
	}

	public override void OnBeforeGenericTypeDefined(IReadOnlyDictionary<string, TypeBase> changedGenerics)
	{
		if (Type is not UndefinedGenericType undefined)
			return;

		if (changedGenerics.TryGetValue(undefined.Name, out var newType))
		{
			Type = newType;
			Decorations[typeof(CurrentUndefinedType)] = new CurrentUndefinedType(Type);
		}
	}

	protected override void Deserialize(SerializedNode serializedNodeObj)
	{
		base.Deserialize(serializedNodeObj);

		Type = ((CurrentUndefinedType)(Decorations[typeof(CurrentUndefinedType)])).Type;
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		info.LocalVariables[Outputs[0]] = Expression.Constant(Type.MakeRealType());
	}

}
