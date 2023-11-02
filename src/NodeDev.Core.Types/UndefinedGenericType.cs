using System.Text.Json;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NodeDev.Core.Types.Tests")]

namespace NodeDev.Core.Types;


public class UndefinedGenericType : TypeBase
{
	private record class SerializedUndefinedGenericType(string Name, Guid Id);

	public readonly Guid Id;

	public override string Name { get; }

	public override string FullName { get; }

        public override TypeBase[] Generics => Array.Empty<TypeBase>();

	public override string FriendlyName => Name;

	public override TypeBase? BaseType => throw new NotImplementedException();

	public override IEnumerable<TypeBase> Interfaces => throw new NotImplementedException();


	internal UndefinedGenericType(string name, Guid id) : base()
	{
		FullName = Name = name;
		Id = id;
	}

	internal protected override string Serialize() => JsonSerializer.Serialize(new SerializedUndefinedGenericType(Name, Id));

	public new static UndefinedGenericType Deserialize(TypeFactory typeFactory, string serialized)
	{
		var deserialized = JsonSerializer.Deserialize<SerializedUndefinedGenericType>(serialized) ?? throw new Exception("Unable to deserialize UndefinedGenericType");
		if(typeFactory.ExistingUndefinedGenericTypes.TryGetValue(deserialized.Id, out var existing))
			return existing;

		return typeFactory.CreateUndefinedGenericType(deserialized.Name);
	}

	//public override bool IsAssignableTo(TypeBase other)
	//{
	//	throw new NotImplementedException();
	//}
	//
	//public override bool IsSame(TypeBase other, bool ignoreGenerics)
	//{
	//	throw new NotImplementedException();
	//}
}
