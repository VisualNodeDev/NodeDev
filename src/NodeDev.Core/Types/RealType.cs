using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Types;

public class RealType : TypeBase
{
    private readonly Type BackendType;

    public override string Name => BackendType.Name;

    public override string FullName => BackendType.FullName!;

    public override bool IsClass => BackendType.IsClass;

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

    internal RealType(Type backendType)
    {
        BackendType = backendType;
    }

    internal override string Serialize()
    {
        return FullName;
    }

    public static RealType Deserialize(string fullName)
    {
        var type = Type.GetType(fullName) ?? throw new Exception($"Type not found {fullName}"); ;

        return TypeFactory.Get(type);
    }

    public override object? ParseTextboxEdit(string text)
    {
        return Convert.ChangeType(text, BackendType);
    }

}
