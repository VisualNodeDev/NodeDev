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

    public override UndefinedGenericType ArrayType => new(UndefinedGenericTypeName, NbArrayLevels + 1);

    public override UndefinedGenericType ArrayInnerType => IsArray ? new UndefinedGenericType(UndefinedGenericTypeName, NbArrayLevels - 1) : throw new Exception("Can't call ArrayInnerType on non-array type");

    public readonly string UndefinedGenericTypeName;

    public UndefinedGenericType(string name, int nbArrayLevels = 0)
    {
        UndefinedGenericTypeName = name;
        FullName = Name = name + NodeClassArrayType.GetArrayString(nbArrayLevels);
        NbArrayLevels = nbArrayLevels;
    }

    /// <summary>
    /// Simplifies the current undefined generic to match as easily as possible with the other type.
    /// T[] to string[] will return string. T to string[] will return string[].
    /// </summary>
    public TypeBase SimplifyToMatchWith(TypeBase otherType)
    {
        var thisUndefined = this;
        while(thisUndefined.IsArray)
        {
            if(!otherType.IsArray)
                throw new Exception("Can't simplify array to non-array type");

            thisUndefined = thisUndefined.ArrayInnerType;
            otherType = otherType.ArrayInnerType;
        }

        return otherType;
    }


    public override IEnumerable<IMethodInfo> GetMethods() => [];

    public override IEnumerable<IMethodInfo> GetMethods(string name) => [];

    #region Serialize / Deserialize

    internal protected override string Serialize() => JsonSerializer.Serialize(new SerializedUndefinedGenericType(UndefinedGenericTypeName, NbArrayLevels));
    public static UndefinedGenericType Deserialize(TypeFactory typeFactory, string serialized)
    {
        var deserialized = JsonSerializer.Deserialize<SerializedUndefinedGenericType>(serialized) ?? throw new Exception("Unable to deserialize UndefinedGenericType");

        return new UndefinedGenericType(deserialized.Name, deserialized.NbArrayLevels);
    }

    #endregion

    public override Type MakeRealType()
    {
        throw new Exception("Unable to make real type with undefined generics");
    }

    public override bool IsSameBackend(TypeBase typeBase)
    {
        if (typeBase is not UndefinedGenericType undefinedGenericType)
            return false;

        return Name == undefinedGenericType.Name;
    }
}
