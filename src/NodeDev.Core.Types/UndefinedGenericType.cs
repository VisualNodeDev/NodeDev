using System.Text.Json;


namespace NodeDev.Core.Types;


public class UndefinedGenericType : TypeBase
{
	private record class SerializedUndefinedGenericType(string Name);


	public override string Name { get; }

	public override string FullName { get; }

	public override TypeBase[] Generics => Array.Empty<TypeBase>();

	public override string FriendlyName => Name;

	public override TypeBase? BaseType => throw new NotImplementedException();

	public override TypeBase[] Interfaces => throw new NotImplementedException();

	internal UndefinedGenericType(string name)
	{
		FullName = Name = name;
	}

	internal protected override string Serialize() => JsonSerializer.Serialize(new SerializedUndefinedGenericType(Name));

	public new static UndefinedGenericType Deserialize(TypeFactory typeFactory, string serialized)
	{
		var deserialized = JsonSerializer.Deserialize<SerializedUndefinedGenericType>(serialized) ?? throw new Exception("Unable to deserialize UndefinedGenericType");

		return new(deserialized.Name);
	}

	public override Type MakeRealType()
	{
		throw new Exception("Unable to make real type with undefined generics");
	}

	public override bool IsSameBackend(TypeBase typeBase)
	{
		return Name == typeBase.Name;
	}
}
