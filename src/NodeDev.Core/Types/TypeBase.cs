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

	public abstract TypeBase[] Interfaces { get; }

	public bool HasUndefinedGenerics => Generics.Any(x => x is UndefinedGenericType || x.HasUndefinedGenerics);

	public virtual bool IsExec => false;

	public virtual bool AllowTextboxEdit => false;

	public virtual string? DefaultTextboxValue => null;

	public abstract string FriendlyName { get; }

	internal protected abstract string Serialize();

	public abstract Type MakeRealType();

	public abstract TypeBase CloneWithGenerics(TypeBase[] newGenerics);

	public virtual object? ParseTextboxEdit(string text) => throw new NotImplementedException();

	public abstract IEnumerable<IMemberInfo> GetMembers();

	private record class SerializedType(string TypeFullName, string SerializedTypeCustom);
	public string SerializeWithFullTypeName()
	{
		var serializedType = new SerializedType(GetType().FullName!, Serialize());

		return System.Text.Json.JsonSerializer.Serialize(serializedType);
	}

	public IEnumerable<UndefinedGenericType> GetUndefinedGenericTypes()
	{
		IEnumerable<UndefinedGenericType> undefinedGenericTypes = this is UndefinedGenericType undefinedGenericType ? new[] { undefinedGenericType } : Enumerable.Empty<UndefinedGenericType>();

		return undefinedGenericTypes.Concat(Generics.SelectMany(x => x.GetUndefinedGenericTypes())).Distinct();
	}

	public TypeBase ReplaceUndefinedGeneric(IReadOnlyDictionary<UndefinedGenericType, TypeBase> genericTypes)
	{
		var generics = new TypeBase[Generics.Length];

        for (int i = 0; i < Generics.Length; ++i)
		{
			var generic = Generics[i];
			if (generic is UndefinedGenericType undefinedGenericType)
			{
				if(genericTypes.TryGetValue(undefinedGenericType, out var newType))
					generics[i] = newType;
				else
					generics[i] = undefinedGenericType; // put back the undefined generic if we didn't find a replacement
			}
			else
				generics[i] = generic.ReplaceUndefinedGeneric(genericTypes); // ask a more complex type to replace its own undefined generics
		}

		return CloneWithGenerics(generics);
	}

	#region Assignation checks

	/// <summary>
	/// Returns in the backend type is the same, ignoring generics
	/// For RealType, that means the actual 'Type' Backend 
	/// </summary>
	/// <param name="typeBase"></param>
	/// <returns></returns>
	public abstract bool IsSameBackend(TypeBase typeBase);

	internal bool IsDirectlyAssignableTo(TypeBase other, bool allowInOutGenerics, [MaybeNullWhen(false)] out Dictionary<UndefinedGenericType, TypeBase> changedGenerics)
	{
		if (this is UndefinedGenericType thisUndefinedGenericType)
		{
			if (other is UndefinedGenericType)
			{
				changedGenerics = new(); // nothing to change, we're plugging a generic into another generic
				return true;
			}
			else // we can change 'this' to the same type as 'other' and plug into it
			{
				changedGenerics = new()
				{
					[thisUndefinedGenericType] = other
				};
				return true;
			}

		}
		else if (other is UndefinedGenericType otherUndefinedGenericType) // we can change the other generic into the current type
		{
			changedGenerics = new()
			{
				[otherUndefinedGenericType] = this
			};
			return true;
		}

		if (IsSameBackend(other)) // same backend, List<int> would be the same backend as List<string> or List<T>
		{
			// check all the generics, they have to either be undefined, the same or covariant
			changedGenerics = new();
			for (int i = 0; i < Generics.Length; ++i)
			{
				if (allowInOutGenerics && other.IsOut(i)) // we can plug a less derived type, like IEnumerable<Child> to IEnumerable<Parent>
				{
					if (Generics[i].IsAssignableTo(other.Generics[i], out var changedGenericsLocal))
					{
						foreach (var changed in changedGenericsLocal)
							changedGenerics[changed.Key] = changed.Value;
						continue; // it worked, check the next generic
					}
				}
				else if (allowInOutGenerics && other.IsIn(i)) // We can plug a more derived type, like IComparable<Parent> to IComparable<Child>
				{
					if (other.Generics[i].IsAssignableTo(Generics[i], out var changedGenericsLocal))
					{
						foreach (var changed in changedGenericsLocal)
							changedGenerics[changed.Key] = changed.Value;
						continue; // it worked, check the next generic
					}
				}
				else
				{
					// the generic is not covariant, so it has to be the same
					if (Generics[i].IsDirectlyAssignableTo(other.Generics[i], allowInOutGenerics, out var changedGenericsLocal))
					{
						foreach (var changed in changedGenericsLocal)
							changedGenerics[changed.Key] = changed.Value;
						continue; // it worked, check the next generic
					}
				}


				// one of the generics is not assignable, so we're not assignable
				changedGenerics = null;
				return false;
			}

			return true;
		}

		changedGenerics = null;
		return false;
	}

	public bool IsAssignableTo(TypeBase other, [MaybeNullWhen(false)] out Dictionary<UndefinedGenericType, TypeBase> changedGenerics)
	{
		if (IsDirectlyAssignableTo(other, true, out changedGenerics))
			return true; // Either plugging something easy like List<int> to List<int>, some generic or covariant like IEnumerable<Parent> to IEnumerable<Child>

		var myAssignableTypes = GetAssignableTypes().OrderBy(x => x.Depth).Select(x => x.Type).ToList();

		var changedGenericsLocal = new Dictionary<UndefinedGenericType, TypeBase>();
		foreach (var myAssignableType in myAssignableTypes)
		{
			if (!myAssignableType.IsSameBackend(other))
				continue;

			if (myAssignableType.Generics.Length != other.Generics.Length)
				throw new Exception("Unable to compare types with different number of generics, this should never happen since the BackendType is identical");

			bool isAssignable = true;
			changedGenericsLocal.Clear(); // reuse the same dictionary for optimisation purposes
			for (int i = 0; i < myAssignableType.Generics.Length; i++)
			{
				// check if either, both or none of the current and other type are undefined generics
				if (myAssignableType.Generics[i] is UndefinedGenericType currentUndefinedGeneric)
				{
					if (myAssignableType.Generics[i] is UndefinedGenericType)
						continue; // this works out fine, we're plugging a generic into another generic
					else // we're plugging a generic into a non generic type, just we record that and keep checking the others
					{
						changedGenericsLocal[currentUndefinedGeneric] = myAssignableType.Generics[i];
						continue;
					}
				}
				else if (other.Generics[i] is UndefinedGenericType otherUndefinedGeneric)
				{
					// we know we're not a generic, so we're plugging a type into a generic
					changedGenericsLocal[otherUndefinedGeneric] = myAssignableType.Generics[i];
					continue;
				}

				bool checkParents = true;
				TypeBase left, right;
				// let's check if they have the same backend type, like List<T> and List<int> share the List<> backend type
				// if they are 2 types that could be assigned only by looking at their BaseType or interfaces (Like List<int> to IEnumerable<int>), that will be checked later
				if (!myAssignableType.Generics[i].IsSameBackend(other.Generics[i]))
				{
					// Check if the types are assignable even if the generic isn't the same
					// By example, List<ParentClass> can be assigned to IEnumerable<ChildClass> even though ParentClass and ChildClass aren't the same
					if (other.IsOut(i))
					{
						// we have to check if otherAssignableType.Generics[i] is less derived than myAssignableType.Generics[i]
						left = myAssignableType.Generics[i];
						right = other.Generics[i];
					}
					else if (other.IsIn(i))
					{
						// we have to check if otherAssignableType.Generics[i] is more derived than myAssignableType.Generics[i]
						left = other.Generics[i];
						right = myAssignableType.Generics[i];
					}
					else
					{
						// we have to check if otherAssignableType.Generics[i] is the same as myAssignableType.Generics[i]
						left = myAssignableType.Generics[i];
						right = other.Generics[i];
						checkParents = false;
					}
				}
				else
				{
					left = myAssignableType.Generics[i];
					right = other.Generics[i];
					checkParents = false;
				}

				if (checkParents)
				{
					if (left.IsAssignableTo(right, out var changedGenericsLocally))
					{
						foreach (var changed in changedGenericsLocally)
							changedGenericsLocal[changed.Key] = changed.Value;
						continue;
					}
				}
				else
				{
					if (left.IsDirectlyAssignableTo(right, false, out var changedGenericsLocally))
					{
						foreach (var changed in changedGenericsLocally)
							changedGenericsLocal[changed.Key] = changed.Value;
						continue;
					}
				}


				isAssignable = false;
				break;
			}

			if (isAssignable)
			{
				changedGenerics = changedGenericsLocal;
				return true;
			}
		}


		changedGenerics = null;
		return false;
	}

	#endregion

	/// <summary>
	/// If true, We can plug any less derived type. Ex IComparer<Person> can be assigned to IComparer<Employee> even though a person is not necessarily an employee. This one is unusual
	/// </summary>
	public virtual bool IsIn(int genericIndex) => false;

	/// <summary>
	/// If true, we can plug any derived type. Ex List<Employee> ca be assigned to IEnumerable<Person>
	/// </summary>
	public virtual bool IsOut(int genericIndex) => false;

	/// <summary>
	/// Returns a list of types that this type can be assigned to
	/// </summary>
	public IEnumerable<(TypeBase Type, int Depth)> GetAssignableTypes()
	{
		return GetAssignableTypes(0).DistinctBy(x => x.Type);
	}

	private IEnumerable<(TypeBase Type, int Depth)> GetAssignableTypes(int depth)
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
