using System.Text.Json;


namespace NodeDev.Core.Types;


public class UndefinedGenericType : TypeBase
{
    private record class SerializedUndefinedGenericType(string Name, int NbArrayLevels = 0);

    public int NbArrayLevels { get; }

    public override string Name { get; }

    public override string FullName { get; }

    public override TypeBase[] Generics => [];

    public override string FriendlyName => Name;

    public override TypeBase? BaseType => throw new NotImplementedException();

    public override TypeBase[] Interfaces => throw new NotImplementedException();

    public override TypeBase CloneWithGenerics(TypeBase[] newGenerics) => throw new NotImplementedException();

    public override IEnumerable<IMemberInfo> GetMembers() => throw new NotImplementedException();

    public override bool IsArray => NbArrayLevels != 0;

    public override TypeBase ArrayType => new UndefinedGenericType(Name, NbArrayLevels + 1);

    public override TypeBase ArrayInnerType
    {
        get
        {
            if (NbArrayLevels == 1)
                return new UndefinedGenericType(Name);
            else if(NbArrayLevels > 1)
                return new UndefinedGenericType(Name, NbArrayLevels - 1);

            throw new Exception("Can't call ArrayInnerType on non-array type");
        }
    }


    public UndefinedGenericType(string name, int nbArrayLevels = 0)
    {
        FullName = Name = name + NodeClassArrayType.GetArrayString(nbArrayLevels);
        NbArrayLevels = nbArrayLevels;
    }

    public override IEnumerable<IMethodInfo> GetMethods() => [];

    public override IEnumerable<IMethodInfo> GetMethods(string name) => [];

    internal protected override string Serialize() => JsonSerializer.Serialize(new SerializedUndefinedGenericType(Name, NbArrayLevels));

    public new static UndefinedGenericType Deserialize(TypeFactory typeFactory, string serialized)
    {
        var deserialized = JsonSerializer.Deserialize<SerializedUndefinedGenericType>(serialized) ?? throw new Exception("Unable to deserialize UndefinedGenericType");

        return new(deserialized.Name, deserialized.NbArrayLevels);
    }

    public override Type MakeRealType()
    {
        throw new Exception("Unable to make real type with undefined generics");
    }

    public override bool IsSameBackend(TypeBase typeBase)
    {
        if (typeBase is not UndefinedGenericType undefinedGenericType)
            return false;

        return Name == undefinedGenericType.Name && NbArrayLevels == undefinedGenericType.NbArrayLevels;
    }
}
