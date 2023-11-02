using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Types;

public abstract class TypeBase
{
	public abstract string Name { get; }

	public abstract string FullName { get; }

	public virtual bool IsClass => true;

	public abstract TypeBase[] Generics { get; }

	public abstract TypeBase? BaseType { get; }

	public abstract IEnumerable<TypeBase> Interfaces { get; }

	public bool HasUndefinedGenerics => Generics.Any(x => x.HasUndefinedGenerics || x is UndefinedGenericType);

	public virtual bool IsExec => false;

	public virtual bool AllowTextboxEdit => false;

	public virtual string? DefaultTextboxValue => null;

	public abstract string FriendlyName { get; }

	internal protected abstract string Serialize();

	private record class SerializedType(string TypeFullName, string SerializedTypeCustom);
	public string SerializeWithFullTypeName()
	{
		var serializedType = new SerializedType(GetType().FullName!, Serialize());

		return System.Text.Json.JsonSerializer.Serialize(serializedType);
	}

	private IEnumerable<(TypeBase Type, int Depth)> GetAssignableTypes(int depth = 0)
	{
		yield return (this, depth);

		if (BaseType != null)
		{
			foreach (var baseType in BaseType.GetAssignableTypes(depth + 1))
				yield return baseType;
		}

		foreach (var @interface in Interfaces)
		{
			foreach (var interfaceType in @interface.GetAssignableTypes(depth + 1))
				yield return interfaceType;
		}
	}

	public virtual IEnumerable<IMethodInfo> GetMethods() => Enumerable.Empty<IMethodInfo>();

	public static TypeBase Deserialize(TypeFactory typeFactory, string serialized)
	{
		var serializedType = System.Text.Json.JsonSerializer.Deserialize<SerializedType>(serialized) ?? throw new Exception("Unable to deserialize type");

		var type = typeFactory.GetTypeByFullName(serializedType.TypeFullName) ?? throw new Exception($"Type not found: {serializedType.TypeFullName}");

		var deserializeMethod = type.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static) ?? throw new Exception($"Deserialize method not found in type: {serializedType.TypeFullName}");

		var deserializedType = deserializeMethod.Invoke(null, new object[] { typeFactory, serializedType.SerializedTypeCustom });

		if (deserializedType is TypeBase typeBase)
			return typeBase;

		throw new Exception($"Deserialize method in type {serializedType.TypeFullName} returned invalid type");
	}

}
