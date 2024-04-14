
using System.Text;

namespace NodeDev.Core.Types;

public class TypeFactory
{
	public List<string> IncludedNamespaces = new()
	{
		"System",
		"System.Collections.Generic",
		"System.Linq",
		"System.Text",
		"System.Threading",
		"System.Threading.Tasks",
	};
	private Dictionary<string, List<string>> TypeCorrespondances = new()
	{
		["System.Int32"] = new() { "int" },
		["System.Int64"] = new() { "long" },
		["System.Single"] = new() { "float" },
		["System.Double"] = new() { "double" },
		["System.Boolean"] = new() { "bool" },
		["System.String"] = new() { "string" },
		["System.Void"] = new() { "void" },
	};

	private ExecType ExecType_;

	private readonly Dictionary<Type, RealType> FullyConstructedRealTypes = new();
	private readonly Dictionary<string, RealType> RealTypesWithPendingGenerics = new();

	public readonly Project Project;

	public TypeFactory(Project project)
	{
		ExecType_ = new();
		Project = project;
	}

	public ExecType ExecType => ExecType_;

	public RealType Get<T>() => Get(typeof(T), null);
	public RealType Get(Type type, TypeBase[]? generics)
	{
		if (generics == null || generics.Length == 0)
		{
			if (!FullyConstructedRealTypes.TryGetValue(type, out var realType))
			{
				realType = new RealType(this, type, null);
				FullyConstructedRealTypes[type] = realType;
			}

			return realType;
		}

		// bad luck, there are generics in here, we need to construct a unique key for the type and the generics so we can check the cache
		var sb = new StringBuilder();
		sb.Append(type.GetGenericTypeDefinition().GetHashCode()); // start with the type hash code

		foreach(var generic in generics)
			sb.Append(generic.GetHashCode()); // add the hash code of each generic

		var key = sb.ToString();
		if(!RealTypesWithPendingGenerics.TryGetValue(key, out var realTypeWithGenerics))
		{
			realTypeWithGenerics = new RealType(this, type, generics);
			RealTypesWithPendingGenerics[key] = realTypeWithGenerics;
		}

		return realTypeWithGenerics;
	}

	public Type? GetTypeByFullName(string name)
	{
		return AppDomain.CurrentDomain.GetAssemblies().Select(x => x.GetType(name)).FirstOrDefault(x => x != null);
	}

	private Type? GetTypeFromAllAssemblies(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			return null;

		var assemblies = AppDomain.CurrentDomain.GetAssemblies();
		return assemblies.Select(ass => ass.GetType(name, false, true)).FirstOrDefault(x => x != null);
	}

	#region CreateBaseFromUserInput

	public string? CreateBaseFromUserInput(string typeName, out TypeBase? type)
	{
		typeName = typeName.Replace(" ", "");
		if (typeName.Count(c => c == '<') != typeName.Count(c => c == '>'))
		{
			type = null;
			return "Bracket opened but never closed";
		}

		if (!typeName.Any(c => c == '<'))
		{
			// easy, just find the type, it's either a full name or a name we can find in the included namespaces
			var correspondance = TypeCorrespondances.FirstOrDefault(x => x.Value.Contains(typeName));
			if (correspondance.Key != null)
				typeName = correspondance.Key;
			var currentRealType = GetTypeFromAllAssemblies(typeName) ?? IncludedNamespaces.Select(ns => GetTypeFromAllAssemblies($"{ns}.{typeName}")).FirstOrDefault(t => t != null);

			if (currentRealType?.IsGenericType == true)
			{
				type = null;
				return "Not all generics are provided for type:" + typeName;
			}
			else if (currentRealType != null)
			{
				type = Get(currentRealType, null);
				return null;
			}
			else if (currentRealType == null)
			{
				var nodeClass = Project.Classes.FirstOrDefault(x => x.Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
				if (nodeClass != null)
				{
					type = nodeClass.ClassTypeBase;
					return null;
				}
			}

			type = null;
			return $"Type {typeName} not found";
		}

		// we have a generic type, we need to find the base type and the generic arguments
		var name = typeName[..typeName.IndexOf('<')];
		var genericArgs = typeName[(typeName.IndexOf('<') + 1)..^1].Split(',').Select(s => s.Trim()).ToArray();

		// find the base type
		var correspondance2 = TypeCorrespondances.FirstOrDefault(x => x.Value.Contains(name));
		if (correspondance2.Key != null)
			name = correspondance2.Key;

		var baseType = GetTypeFromAllAssemblies(name + "`" + genericArgs.Length) ?? IncludedNamespaces.Select(ns => GetTypeFromAllAssemblies($"{ns}.{name}`{genericArgs.Length}")).FirstOrDefault(t => t != null);
		if (baseType == null)
		{
			type = null;
			return $"Type {name} not found";
		}

		// find the generic arguments
		var genericArgsTypes = new TypeBase[genericArgs.Length];
		for (int i = 0; i < genericArgs.Length; i++)
		{
			var error = CreateBaseFromUserInput(genericArgs[i], out var genericArgType);
			if (error != null)
			{
				type = null;
				return error;
			}
			genericArgsTypes[i] = genericArgType!;
		}

		// create the generic type
		type = Get(baseType, genericArgsTypes);
		return null;
	}

	#endregion
}
