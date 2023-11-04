using System.Reflection;

namespace NodeDev.Core.Types;

public class RealMethodInfo : IMethodInfo
{
	public readonly TypeFactory TypeFactory;

	public readonly MethodInfo Method;

	public string Name => Method.Name;

	public bool IsStatic => Method.IsStatic;

	public TypeBase DeclaringType => DeclaringRealType;

	public RealType DeclaringRealType { get; }

	public TypeBase ReturnType
	{
		get
		{
			if(Method.ReturnType.IsGenericParameter)
				return DeclaringRealType.Generics[Method.ReturnType.GenericParameterPosition];
			return TypeFactory.Get(Method.ReturnType, null);
		}
	}

	public RealMethodInfo(TypeFactory typeFactory, MethodInfo method, RealType declaringType)
	{
		TypeFactory = typeFactory;
		Method = method;
		DeclaringRealType = declaringType;
	}

	public IEnumerable<IMethodParameterInfo> GetParameters()
	{
		return Method.GetParameters().Select(x => new RealMethodParameterInfo(x, TypeFactory, DeclaringRealType));
	}
}
