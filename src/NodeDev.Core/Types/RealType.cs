using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Types;

[DebuggerDisplay("RealType: {FriendlyName}")]
public class RealType : TypeBase
{
	internal readonly Type BackendType;

	internal readonly TypeFactory TypeFactory;

	public override string Name => BackendType.Name;

	public override string FullName => BackendType.FullName!;

	public override bool IsClass => BackendType.IsClass;

	public override TypeBase? BaseType { get; }

	public override TypeBase[] Generics { get; }

	private TypeBase[]? _Interfaces;
	public override TypeBase[] Interfaces => _Interfaces ?? InitializeInterfaces();

	public override bool IsIn(int genericIndex) => BackendType.GetGenericArguments()[genericIndex].IsGenericParameter && (BackendType.GetGenericArguments()[genericIndex].GenericParameterAttributes & System.Reflection.GenericParameterAttributes.Contravariant) !=  System.Reflection.GenericParameterAttributes.None;
	
	public override bool IsOut(int genericIndex) => BackendType.GetGenericArguments()[genericIndex].IsGenericParameter && (BackendType.GetGenericArguments()[genericIndex].GenericParameterAttributes & System.Reflection.GenericParameterAttributes.Covariant) !=  System.Reflection.GenericParameterAttributes.None;

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

	public override IEnumerable<IMemberInfo> GetMembers()
	{
		var properties = BackendType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
		var fields = BackendType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

		return properties.Select(x => new RealMemberInfo(x, this)).Concat(fields.Select(x => new RealMemberInfo(x, this)));
	}

	public override TypeBase CloneWithGenerics(TypeBase[] newGenerics)
	{
		if (newGenerics.Length != Generics.Length)
			throw new Exception("Unable to clone type with different number of generics");

		return TypeFactory.Get(BackendType, newGenerics);
	}

	public override Type MakeRealType()
	{
		if (Generics.Length == 0)
			return BackendType;

		if (HasUndefinedGenerics)
			throw new Exception("Unable to make real type with undefined generics");

		return BackendType.MakeGenericType(Generics.Select(x => x.MakeRealType()).ToArray());
	}

	private string GetFriendlyName(TypeBase t)
	{
		if(t is RealType realType)
			return realType.FriendlyName;
		return t.Name;
	}

	private string GetFriendlyName(Type t)
	{
		if (Generics.Length == 0)
			return t.Name;

		// return the name of 't' without the ` and the number, replaced with the actual generic type names
		var name = t.Name[..t.Name.IndexOf('`')];
		return $"{name}<{string.Join(", ", Generics.Select(GetFriendlyName))}>";
	}
	public override string FriendlyName => GetFriendlyName(BackendType);

	public override IEnumerable<IMethodInfo> GetMethods()
	{
		return BackendType.GetMethods().Select(x => new RealMethodInfo(TypeFactory, x, this));
	}


	internal RealType(TypeFactory typeFactory, Type backendType, TypeBase[]? generics)
	{
		TypeFactory = typeFactory;

		if (generics == null)
		{
			if (backendType.IsGenericType && !backendType.IsConstructedGenericType)
				throw new Exception("Unable to create real type with undefined generics. To do so you must manually specify the generics through the 'generics' parameter in the RealType constructor");
			Generics = backendType.GetGenericArguments().Select(x => TypeFactory.Get(x, null)).ToArray();
		}
		else
			Generics = generics;

		BackendType = backendType;
		// make sure we always store the generic type, so List<> instead of List<int>
		if (BackendType.IsGenericType)
		{
			BackendType = BackendType.GetGenericTypeDefinition();

			BaseType = BackendType.BaseType == null || BackendType.BaseType == typeof(object) ? null : MatchGenericTypes(BackendType.BaseType);
		}
		else
			BaseType = BackendType.BaseType == null || BackendType.BaseType == typeof(object) ? null : TypeFactory.Get(BackendType.BaseType, null);
	}

	private TypeBase[] InitializeInterfaces()
	{
		if (BackendType.IsGenericType)
			_Interfaces = BackendType.GetInterfaces().Select(MatchGenericTypes).ToArray();
		else
			_Interfaces = BackendType.GetInterfaces().Select(x => TypeFactory.Get(x, null)).ToArray();

		return _Interfaces;
	}

	/// match the generics to the interfaces
	/// Example, if we have List<T>: IList<T>, we must make sure the Generics[0] is the same as the T in the IList<T> generic
	/// To do that, we can match the name 'T' between List and IList, and the index of 'T' in List<T> to the index on the Generics array
	private TypeBase MatchGenericTypes(Type typeUsingOurGenerics)
	{
		var ourGenerics = BackendType.GetGenericArguments().ToList();
		var theirGenerics = typeUsingOurGenerics.GetGenericArguments();

		var generics = new TypeBase[theirGenerics.Length];

		for (int i = 0; i < theirGenerics.Length; i++)
		{
			var theirGeneric = theirGenerics[i];
			var ourGeneric = ourGenerics.FindIndex(x => x.Name == theirGeneric.Name);
			// if it is not found, it means the type is just specified directly 
			// Ex, in the case of Dictionary<int, T>, the 'int' will be specified directly, and 'T' will be found in ourGenerics
			if (ourGeneric == -1)
				generics[i] = TypeFactory.Get(theirGeneric, null);
			else
				generics[i] = Generics[ourGeneric];
		}

		return new RealType(TypeFactory, typeUsingOurGenerics, generics);
	}

	private record class SerializedType(string TypeFullName, string[] SerializedGenerics);
	internal protected override string Serialize()
	{
		return System.Text.Json.JsonSerializer.Serialize(new SerializedType(BackendType.FullName!, Generics.Select(x => x.SerializeWithFullTypeName()).ToArray()));
	}

	public new static RealType Deserialize(TypeFactory typeFactory, string serializedString)
	{
		var serializedType = System.Text.Json.JsonSerializer.Deserialize<SerializedType>(serializedString) ?? throw new Exception("Unable to deserialize type");

		var type = typeFactory.GetTypeByFullName(serializedType.TypeFullName) ?? throw new Exception($"Type not found: {serializedType.TypeFullName}");

		var generics = serializedType.SerializedGenerics.Select(x => TypeBase.Deserialize(typeFactory, x)).ToArray();

		return typeFactory.Get(type, generics);
	}

	public override object? ParseTextboxEdit(string text)
	{
	    return Convert.ChangeType(text, BackendType);
	}

	public override bool IsSameBackend(TypeBase other)
	{
		return other is RealType realType && realType.BackendType == BackendType;
	}

}
