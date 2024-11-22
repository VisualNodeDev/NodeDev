using System.Reflection;

namespace NodeDev.Core.Types;


public class RealMethodParameterInfo : IMethodParameterInfo
{
	public readonly ParameterInfo ParameterInfo;

	private readonly RealType DeclaringRealType;

	public readonly TypeFactory TypeFactory;

	public RealMethodParameterInfo(ParameterInfo parameterInfo, TypeFactory typeFactory, RealType declaringRealType)
	{
		ParameterInfo = parameterInfo;
		TypeFactory = typeFactory;
		DeclaringRealType = declaringRealType;
	}

	public string Name => ParameterInfo.Name ?? "";

	private IEnumerable<TypeBase> ReplaceGenericsRecursively(Type type)
	{
		foreach (var generic in type.GetGenericArguments())
		{
			if (generic.IsGenericMethodParameter)
				yield return TypeFactory.Get(generic, null);
			else if (generic.IsGenericParameter)
				yield return DeclaringRealType.Generics[generic.GenericParameterPosition];
			else if (!generic.IsGenericType) // we've reached the end, that one is good, we can simply return it
				yield return TypeFactory.Get(generic, null);
			else
			{
				var generics = ReplaceGenericsRecursively(generic).ToArray();
				yield return TypeFactory.Get(generic, generics);
			}
		}
	}

	public TypeBase ParameterType
	{
		get
		{
			if (ParameterInfo.ParameterType.IsGenericMethodParameter)
				return TypeFactory.Get(ParameterInfo.ParameterType, null);
			else if (ParameterInfo.ParameterType.IsGenericParameter)
				return DeclaringRealType.Generics[ParameterInfo.ParameterType.GenericParameterPosition];
			else if (ParameterInfo.ParameterType.IsGenericType)
			{
				var generics = ReplaceGenericsRecursively(ParameterInfo.ParameterType).ToArray();
				return TypeFactory.Get(ParameterInfo.ParameterType, generics);
			}

			return TypeFactory.Get(ParameterInfo.ParameterType, null);
		}
	}

	public bool IsOut => ParameterInfo.IsOut;
}
