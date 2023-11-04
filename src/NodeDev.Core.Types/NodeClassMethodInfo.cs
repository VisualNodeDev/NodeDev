using NodeDev.Core.Types;
using System.Reflection;

namespace NodeDev.Core.Types;

public interface IMethodInfo
{
	public string Name { get; }

	public bool IsStatic { get; }

	public TypeBase DeclaringType { get; }

	public TypeBase ReturnType { get; }

	public IEnumerable<IMethodParameterInfo> GetParameters();
}

public interface IMethodParameterInfo
{
	public string Name { get; }

	public TypeBase ParameterType { get; }
}


public class RealMethodInfo : IMethodInfo
{
	private readonly TypeFactory TypeFactory;

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
		return Method.GetParameters().Select(x => new RealMethodParameterInfo(x, this));
	}

	public class RealMethodParameterInfo : IMethodParameterInfo
	{
		public readonly ParameterInfo ParameterInfo;

		private readonly RealMethodInfo RealMethodInfo;

		public TypeFactory TypeFactory => RealMethodInfo.TypeFactory;

		public RealMethodParameterInfo(ParameterInfo parameterInfo, RealMethodInfo realMethodInfo)
		{
			ParameterInfo = parameterInfo;
			RealMethodInfo = realMethodInfo;
		}

		public string Name => ParameterInfo.Name ?? "";

		public TypeBase ParameterType
		{
			get
			{
				if(ParameterInfo.ParameterType.IsGenericParameter)
					return RealMethodInfo.DeclaringRealType.Generics[ParameterInfo.ParameterType.GenericParameterPosition];
				return TypeFactory.Get(ParameterInfo.ParameterType, null);
			}
		}
	}
}
