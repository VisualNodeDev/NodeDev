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

	public override TypeBase? BaseType => BackendType.BaseType == null ? null : TypeFactory.Get(BackendType.BaseType);

	public override TypeBase[] Generics { get; }

    public override IEnumerable<TypeBase> Interfaces => BackendType.GetInterfaces().Select(x => TypeFactory.Get(x));

	/// <summary>
	/// Types that the UI will show a textbox for editing
	/// </summary>
	private static readonly List<Type> AllowedEditTypes = new()
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

	public override IEnumerable<IMethodInfo> GetMethods()
	{
        return BackendType.GetMethods().Select(x => new RealMethodInfo(TypeFactory, x));
	}

    private readonly TypeFactory TypeFactory;

	internal RealType(TypeFactory typeFactory, Type backendType, TypeBase[]? generics)
    {
		TypeFactory = typeFactory;
		BackendType = backendType;

        if (generics == null)
            Generics = backendType.GetGenericArguments().Select(x => TypeFactory.Get(x)).ToArray();
        else
            Generics = generics;
	}

    internal protected override string Serialize()
    {
        return FullName;
    }

    public new static RealType Deserialize(TypeFactory typeFactory, string fullName)
    {
        var type = Type.GetType(fullName) ?? throw new Exception($"Type not found {fullName}"); ;

        return typeFactory.Get(type);
    }

    //public override object? ParseTextboxEdit(string text)
    //{
    //    return Convert.ChangeType(text, BackendType);
    //}
    //
	//public override bool IsAssignableTo(TypeBase other)
	//{
    //    if (other is RealType realType)
    //        return BackendType.IsAssignableTo(realType.BackendType);
    //
    //    return false; // a real type cannot inherit from a nodeClass, therefor it can never be assigned to one
	//}
    //
	//public override bool IsSame(TypeBase other, bool ignoreGenerics)
	//{
	//	if (other is RealType realType)
	//	{
	//		if (realType.BackendType == BackendType)
	//		{
	//			if (ignoreGenerics)
	//				return true;
	//			else
	//			{
	//				if (Generics.Length != realType.Generics.Length)
	//					return false;
    //
	//				for (int i = 0; i < Generics.Length; i++)
	//				{
	//					if (!Generics[i].IsSame(realType.Generics[i], ignoreGenerics))
	//						return false;
	//				}
    //
	//				return true;
	//			}
	//		}
	//	}
    //
	//	return false;
	//}

}
