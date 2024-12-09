using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace NodeDev.Core.Types;

public abstract class TypeBase
{
	public abstract string Name { get; }

	public abstract string FullName { get; }

	public virtual bool IsClass => true;

	public abstract TypeBase[] Generics { get; }

	public abstract TypeBase? BaseType { get; }

	public abstract TypeBase[] Interfaces { get; }

	public abstract bool IsArray { get; }

	public abstract TypeBase ArrayType { get; }

	public abstract TypeBase ArrayInnerType { get; }

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

	public record class SerializedType(string TypeFullName, string SerializedTypeCustom);
	public SerializedType SerializeWithFullTypeName()
	{
		var serializedType = new SerializedType(GetType().FullName!, Serialize());

		return serializedType;
	}
	public string SerializeWithFullTypeNameString() => JsonSerializer.Serialize(SerializeWithFullTypeName());

	public IEnumerable<string> GetUndefinedGenericTypes()
	{
		IEnumerable<string> undefinedGenericTypes = this is UndefinedGenericType undefinedGenericType ? [undefinedGenericType.UndefinedGenericTypeName] : [];

		return undefinedGenericTypes.Concat(Generics.SelectMany(x => x.GetUndefinedGenericTypes())).Distinct();
	}

	public TypeBase ReplaceUndefinedGeneric(IReadOnlyDictionary<string, TypeBase> genericTypes)
	{
		if (this is UndefinedGenericType undefinedGeneric)
		{
			if (!genericTypes.TryGetValue(undefinedGeneric.UndefinedGenericTypeName, out var newType))
				return undefinedGeneric; // put back the undefined generic if we didn't find a replacement

			// We know what 'T' is matched with. Now if we have T[], we have to increase the array level of the matched type too.
			for (int i = 0; i < undefinedGeneric.NbArrayLevels; ++i)
				newType = newType.ArrayType;

			return newType;
		}

		var generics = new TypeBase[Generics.Length];

		for (int i = 0; i < Generics.Length; ++i)
			generics[i] = Generics[i].ReplaceUndefinedGeneric(genericTypes); // ask a more complex type to replace its own undefined generics

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

	/// <summary>
	/// Validate if 'this' is directly assignable to 'other' without checkout the entire hierarchy of inheritance and interface implementations
	/// </summary>
	/// <param name="other">Other type we are trying to plug into</param>
	/// <param name="allowInOutGenerics">Check for covariant and contravariant generics</param>
	/// <param name="changedGenericsLeft">Generics that needs to be updated in 'this' in order for the assignation to work</param>
	/// <param name="changedGenericsRight">Generics that needs to be updated in 'other' in order for the assignation to work</param>
	/// <returns></returns>
	internal bool IsDirectlyAssignableTo(TypeBase other, bool allowInOutGenerics, [MaybeNullWhen(false)] out Dictionary<string, TypeBase> changedGenericsLeft, [MaybeNullWhen(false)] out Dictionary<string, TypeBase> changedGenericsRight, out int totalDepths)
	{
		if (this is UndefinedGenericType thisUndefinedGenericType)
		{
			if (other is UndefinedGenericType)
			{
				changedGenericsLeft = []; // nothing to change, we're plugging a generic into another generic
				changedGenericsRight = [];
				totalDepths = 0;
				return true;
			}
			else if (!IsArray || other.IsArray) // we can change 'this' to the same type as 'other' and plug into it
			{
				// We are either plugging:
				// T into string[], T into string or T[] into string[]
				changedGenericsLeft = new()
				{
					[thisUndefinedGenericType.UndefinedGenericTypeName] = thisUndefinedGenericType.SimplifyToMatchWith(other)
				};
				changedGenericsRight = [];
				totalDepths = 0;
				return true;
			}

			changedGenericsLeft = changedGenericsRight = null;
			totalDepths = -1;
			return false;
		}
		else if (other is UndefinedGenericType otherUndefinedGenericType) // we can change the other generic into the current type
		{
			if (IsArray || !other.IsArray)
			{
				// We are either plugging : string[] into T[] or string[] into T
				// OR (the 'if' OR...)
				// We are plugging string string into T
				// In both cases we want to update the generic to the array type and let the system handle the rest later on
				changedGenericsLeft = [];
				changedGenericsRight = new()
				{
					[otherUndefinedGenericType.UndefinedGenericTypeName] = otherUndefinedGenericType.SimplifyToMatchWith(this)
				};
				totalDepths = 0;
				return true;
			}

			// string into T[] <----- this is not allowed
			changedGenericsLeft = changedGenericsRight = null;
			totalDepths = -1;
			return false;
		}

		if (IsSameBackend(other)) // same backend, List<int> would be the same backend as List<string> or List<T>
		{
			// check all the generics, they have to either be undefined, the same or covariant
			changedGenericsLeft = [];
			changedGenericsRight = [];
			totalDepths = 0;
			for (int i = 0; i < Generics.Length; ++i)
			{
				if (allowInOutGenerics && other.IsOut(i)) // we can plug a less derived type, like IEnumerable<Child> to IEnumerable<Parent>
				{
					if (Generics[i].IsAssignableTo(other.Generics[i], out var changedGenericsLeftLocal, out var changedGenericsRightLocal, out var totalSubDepths))
					{
						totalDepths += totalSubDepths;

						foreach (var changed in changedGenericsLeftLocal)
							changedGenericsLeft[changed.Key] = changed.Value;
						foreach (var changed in changedGenericsRightLocal)
							changedGenericsRight[changed.Key] = changed.Value;
						continue; // it worked, check the next generic
					}
				}
				else if (allowInOutGenerics && other.IsIn(i)) // We can plug a more derived type, like IComparable<Parent> to IComparable<Child>
				{
					// Invert the 'changedGenerics' left and right here, since we're plugging 'other' into 'this' this time
					if (other.Generics[i].IsAssignableTo(Generics[i], out var changedGenericsRightLocal, out var changedGenericsLeftLocal, out var totalSubDepths))
					{
						totalDepths += totalSubDepths;

						foreach (var changed in changedGenericsLeftLocal)
							changedGenericsLeft[changed.Key] = changed.Value;
						foreach (var changed in changedGenericsRightLocal)
							changedGenericsRight[changed.Key] = changed.Value;
						continue; // it worked, check the next generic
					}
				}
				else
				{
					// the generic is not covariant, so it has to be the same
					if (Generics[i].IsDirectlyAssignableTo(other.Generics[i], allowInOutGenerics, out var changedGenericsLeftLocal, out var changedGenericsRightLocal, out var totalSubDepths))
					{
						totalDepths += totalSubDepths;

						foreach (var changed in changedGenericsLeftLocal)
							changedGenericsLeft[changed.Key] = changed.Value;
						foreach (var changed in changedGenericsRightLocal)
							changedGenericsRight[changed.Key] = changed.Value;
						continue; // it worked, check the next generic
					}
				}

				// one of the generics is not assignable, so we're not assignable
				changedGenericsLeft = changedGenericsRight = null;
				totalDepths = -1;
				return false;
			}

			return true;
		}

		changedGenericsLeft = changedGenericsRight = null;
		totalDepths = -1;
		return false;
	}

	public bool IsAssignableTo(TypeBase other, [MaybeNullWhen(false)] out Dictionary<string, TypeBase> changedGenericsLeft, [MaybeNullWhen(false)] out Dictionary<string, TypeBase> changedGenericsRight)
	{
		return IsAssignableTo(other, out changedGenericsLeft, out changedGenericsRight, out _);
	}

	/// <summary>
	/// Check if 'this' is assignable to 'other' and return the depth of the assignation.
	/// Also return the generics that needs to be updated in order for the assignation to work.
	/// </summary>
	/// <param name="totalDepths">
	/// Total depths of the assignation. That is, how far up the inheritance tree did we need to go to make this assignation work.
	/// It also sums up the depths of the generics assignations.
	/// This is use to prioritize assignations of lower depth, such as converting List<string> to IList<string> instead of IEnumerable<string>, if possible.
	/// </param>
	/// <returns>True if the assignation is possible. Then <paramref name="changedGenerics"/> and <paramref name="totalDepths"/> are both set.</returns>
	public bool IsAssignableTo(TypeBase other, [MaybeNullWhen(false)] out Dictionary<string, TypeBase> changedGenericsLeft, [MaybeNullWhen(false)] out Dictionary<string, TypeBase> changedGenericsRight, out int totalDepths)
	{
		if ((this is ExecType && other is not ExecType) || (other is ExecType && this is not ExecType))
		{
			changedGenericsLeft = changedGenericsRight = null;
			totalDepths = -1;
			return false;
		}

		if (IsDirectlyAssignableTo(other, true, out changedGenericsLeft, out changedGenericsRight, out var totalSubDepths))
		{
			totalDepths = totalSubDepths;
			return true; // Either plugging something easy like List<int> to List<int>, some generic or covariant like IEnumerable<Parent> to IEnumerable<Child>
		}

		var myAssignableTypes = GetAssignableTypes().OrderBy(x => x.Depth);

		var changedGenericsLeftLocal = new Dictionary<string, TypeBase>();
		var changedGenericsRightLocal = new Dictionary<string, TypeBase>();
		foreach ((var myAssignableType, var assignableDepth) in myAssignableTypes)
		{
			if (!myAssignableType.IsSameBackend(other))
				continue;

			if (myAssignableType.Generics.Length != other.Generics.Length)
				throw new Exception("Unable to compare types with different number of generics, this should never happen since the BackendType is identical");

			var isAssignable = true;
			changedGenericsLeftLocal.Clear(); // reuse the same dictionary for optimization purposes
			changedGenericsRightLocal.Clear(); // reuse the same dictionary for optimization purposes
			var totalDepthsLocal = 0;
			for (int i = 0; i < myAssignableType.Generics.Length; i++)
			{
				// check if either, both or none of the current and other type are undefined generics
				if (myAssignableType.Generics[i] is UndefinedGenericType currentUndefinedGeneric)
				{
					if (other.Generics[i] is UndefinedGenericType)
						continue; // this works out fine, we're plugging a generic into another generic
					else
					{
						// we're plugging a generic into a non generic type, we just record that and keep checking the others
						// however this only works if we're either plugging :
						// T into string or T[] into string[]
						if (currentUndefinedGeneric.IsArray && !other.Generics[i].IsArray)
						{
							isAssignable = false;
							break;
						}
						else
						{
							changedGenericsLeftLocal[currentUndefinedGeneric.UndefinedGenericTypeName] = currentUndefinedGeneric.SimplifyToMatchWith(other.Generics[i]);
							continue;
						}
					}
				}
				else if (other.Generics[i] is UndefinedGenericType otherUndefinedGeneric)
				{
					// we know we're not a generic, so we're plugging a type into a generic
					// However, this only works if we're either plugging :
					// string into T or string[] into T[]
					if (myAssignableType.IsArray && !other.Generics[i].IsArray)
					{
						isAssignable = false;
						break;
					}

					changedGenericsRightLocal[otherUndefinedGeneric.UndefinedGenericTypeName] = otherUndefinedGeneric.SimplifyToMatchWith(myAssignableType.Generics[i]);
					continue;
				}

				bool checkParents = true;
				TypeBase left, right;
				bool swapped = false; // true if left and right changed generics need to be swapped
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
						swapped = true;
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
					// we have to check if otherAssignableType.Generics[i] is the same as myAssignableType.Generics[i]
					left = myAssignableType.Generics[i];
					right = other.Generics[i];
					checkParents = false;
				}

				// Check parents will recursively check if the left is assignable to the right, going up the inheritance tree
				if (checkParents)
				{
					if (left.IsAssignableTo(right, out var changedGenericsLeftLocally, out var changedGenericsRightLocally, out var totalDepths1))
					{
						totalDepthsLocal += totalDepths1;

						if (swapped)
						{
							(changedGenericsRightLocally, changedGenericsLeftLocally) = (changedGenericsLeftLocally, changedGenericsRightLocally);
						}

						foreach (var changed in changedGenericsLeftLocally)
							changedGenericsLeftLocal[changed.Key] = changed.Value;
						foreach (var changed in changedGenericsRightLocally)
							changedGenericsRightLocal[changed.Key] = changed.Value;
						continue;
					}
				}
				else
				{
					if (left.IsDirectlyAssignableTo(right, false, out var changedGenericsLeftLocally, out var changedGenericsRightLocally, out totalSubDepths))
					{
						totalDepthsLocal += totalSubDepths;

						if (swapped)
						{
							(changedGenericsRightLocally, changedGenericsLeftLocally) = (changedGenericsLeftLocally, changedGenericsRightLocally);
						}

						foreach (var changed in changedGenericsLeftLocally)
							changedGenericsLeftLocal[changed.Key] = changed.Value;
						foreach (var changed in changedGenericsRightLocally)
							changedGenericsRightLocal[changed.Key] = changed.Value;
						continue;
					}
				}


				isAssignable = false;
				break;
			}

			if (isAssignable)
			{
				changedGenericsLeft = changedGenericsLeftLocal;
				changedGenericsRight = changedGenericsRightLocal;
				totalDepths = assignableDepth + 1 + totalDepthsLocal;
				return true;
			}
		}


		changedGenericsLeft = changedGenericsRight = null;
		totalDepths = -1;
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

	public abstract IEnumerable<IMethodInfo> GetMethods();

	public IEnumerable<IMethodInfo> GetMethods(MethodAttributes attributes) => GetMethods().Where(x => (x.Attributes & attributes) == attributes);

	public abstract IEnumerable<IMethodInfo> GetMethods(string name);

	public static TypeBase DeserializeFullTypeNameString(TypeFactory typeFactory, string serializedTypeStr)
	{
		var serializedType = JsonSerializer.Deserialize<SerializedType>(serializedTypeStr) ?? throw new Exception("Unable to deserialize type");

		return Deserialize(typeFactory, serializedType);
	}

	public static TypeBase Deserialize(TypeFactory typeFactory, SerializedType serializedType)
	{
		var type = TypeFactory.GetTypeByFullName(serializedType.TypeFullName) ?? throw new Exception($"Type not found: {serializedType.TypeFullName}");

		var deserializeMethod = type.GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static) ?? throw new Exception($"Deserialize method not found in type: {serializedType.TypeFullName}");

		var deserializedType = deserializeMethod.Invoke(null, [typeFactory, serializedType.SerializedTypeCustom]);

		if (deserializedType is TypeBase typeBase)
			return typeBase;

		throw new Exception($"Deserialize method in type {serializedType.TypeFullName} returned invalid type");
	}

}
