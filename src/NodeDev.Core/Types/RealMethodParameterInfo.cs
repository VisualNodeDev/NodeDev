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

	public TypeBase ParameterType
	{
		get
		{
			if (ParameterInfo.ParameterType.IsGenericParameter)
				return DeclaringRealType.Generics[ParameterInfo.ParameterType.GenericParameterPosition];
			return TypeFactory.Get(ParameterInfo.ParameterType, null);
		}
	}
}
