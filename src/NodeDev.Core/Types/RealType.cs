using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Types;

public class RealType : TypeBase
{
    internal readonly Type BackendType;

    public override string Name => BackendType.Name;

    public override string FullName => BackendType.FullName!;

    public override bool IsClass => BackendType.IsClass;

    /// <summary>
    /// Types that the UI will show a textbox for editing
    /// </summary>
    private static List<Type> AllowedEditTypes = new List<Type>()
    {
        typeof(int),
        typeof(string),
        typeof(bool),
        typeof(float),
        typeof(double),
        typeof(decimal),
        typeof(long),
        typeof(short),
        typeof(byte),
        typeof(uint),
        typeof(ulong),
        typeof(ushort),
        typeof(sbyte),
        typeof(char),
        typeof(int?),
        typeof(bool?),
        typeof(float?),
        typeof(double?),
        typeof(decimal?),
        typeof(long?),
        typeof(short?),
        typeof(byte?),
        typeof(uint?),
        typeof(ulong?),
        typeof(ushort?),
        typeof(sbyte?),
        typeof(char?),
    };
    public override bool AllowTextboxEdit => AllowedEditTypes.Contains(BackendType);
    public override string? DefaultTextboxValue
    {
        get
        {
            if (BackendType == typeof(string))
                return null;

            // check if BackendType is Nullable<T>, if so return null. If not, return "0"
            if (BackendType.IsGenericType && BackendType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return null;

            return "0";
        }
    }


    private string GetFriendlyName(Type t)
    {
        // if the type has generics, replace the `1 with the generic type names
        var generics = t.GetGenericArguments();
        if(generics.Length == 0)
            return t.Name;

        // return the name of 't' without the ` and the number, replaced with the actual generic type names
        var name = t.Name[..t.Name.IndexOf('`')];
        return $"{name}<{string.Join(", ", generics.Select(GetFriendlyName))}>";
    }
    public override string FriendlyName => GetFriendlyName(BackendType);

    public override TypeBase[]? Generics => BackendType.GetGenericArguments().Select(TypeFactory.Get).ToArray();

    internal RealType(TypeFactory typeFactory, Type backendType) : base(typeFactory)
    {
        BackendType = backendType;
    }

    internal override string Serialize()
    {
        return FullName;
    }

    public static RealType Deserialize(TypeFactory typeFactory, string fullName)
    {
        var type = Type.GetType(fullName) ?? throw new Exception($"Type not found {fullName}"); ;

        return typeFactory.Get(type);
    }

    public override object? ParseTextboxEdit(string text)
    {
        return Convert.ChangeType(text, BackendType);
    }

}
