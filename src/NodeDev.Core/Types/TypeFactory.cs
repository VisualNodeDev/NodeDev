
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
        "System.Diagnostics",
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

	private readonly Dictionary<string, RealType> RealTypesCache = new(5_000);

	public readonly Project Project;

	public TypeFactory(Project project)
	{
		ExecType_ = new();
		Project = project;
	}

	public ExecType ExecType => ExecType_;

	public RealType Void => Get(typeof(void), null);

	public RealType Get<T>() => Get(typeof(T), null);
	public RealType Get(Type type, TypeBase[]? generics)
	{
		if (generics == null)
		{
			if (type.IsGenericType)
			{
				if (!type.IsConstructedGenericType) // this is something list List<T> instead of List<int>
					throw new Exception("Unable to create real type with undefined generics. To do so you must manually specify the generics through the 'generics' parameter in the RealType constructor");

				generics = type.GetGenericArguments().Select(x => Get(x, null)).ToArray();
				type = type.GetGenericTypeDefinition();
			}
		}
		else if (type.IsGenericType)
			type = type.GetGenericTypeDefinition(); // make sure we always store the generic type, so List<> instead of List<int>

		var sb = new StringBuilder();
		sb.Append(type.GetHashCode()); // start with the type hash code

		if (generics?.Length > 0)
		{
			foreach (var generic in generics)
				sb.Append('-').Append(generic.GetHashCode()); // add the hash code of each generic
		}

		var key = sb.ToString();
		if (!RealTypesCache.TryGetValue(key, out var realTypeWithGenerics))
		{
			realTypeWithGenerics = new RealType(this, type, generics);
			RealTypesCache[key] = realTypeWithGenerics;
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
		int nbArray = 0;
		while(typeName.EndsWith("[]"))
		{
			++nbArray;
			typeName = typeName[..^2];
		}

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
				for (int i = 0; i < nbArray; ++i)
					type = type.ArrayType;

				return null;
			}
			else if (currentRealType == null)
			{
				var nodeClass = Project.Classes.FirstOrDefault(x => x.Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase));
				if (nodeClass != null)
				{
					type = nodeClass.ClassTypeBase;
                    for (int i = 0; i < nbArray; ++i)
                        type = type.ArrayType;

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
        for (int i = 0; i < nbArray; ++i)
            type = type.ArrayType;

        return null;
	}

	#endregion
}
