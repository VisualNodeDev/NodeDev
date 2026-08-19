using System.Reflection;

namespace NodeDev.Core.Types;

public class RealMethodInfo : IMethodInfo
{
	public readonly TypeFactory TypeFactory;

	private readonly MethodInfo Method;

	public string Name => Method.Name;

	public bool IsStatic => Method.IsStatic;

	public TypeBase DeclaringType => DeclaringRealType;

	public RealType DeclaringRealType { get; }

	public TypeBase ReturnType
	{
		get
		{
			if (Method.ReturnType.IsGenericParameter)
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

	internal TypeBase[]? GetClosedGenericArguments()
	{
		if (!Method.IsGenericMethod || Method.IsGenericMethodDefinition)
			return null;

		return Method.GetGenericArguments().Select(type => (TypeBase)TypeFactory.Get(type, null)).ToArray();
	}

	internal RealMethodInfo? CloseGenericMethod(IReadOnlyList<TypeBase> genericArguments)
	{
		if (!Method.IsGenericMethodDefinition || Method.GetGenericArguments().Length != genericArguments.Count)
			return null;

		try
		{
			var closedMethod = Method.MakeGenericMethod(genericArguments.Select(argument => argument.MakeRealType()).ToArray());
			return new RealMethodInfo(TypeFactory, closedMethod, DeclaringRealType);
		}
		catch (ArgumentException)
		{
			return null;
		}
	}

	public MethodInfo CreateMethodInfo()
	{
		if (Method.IsGenericMethod && !Method.ContainsGenericParameters)
			return Method;

		// This seriously needs to be optimized, this will be called a lot and it's slow as hell
		return DeclaringRealType.MakeRealType().GetMethod(Method.Name, GetParameters().Select(x => x.ParameterType.MakeRealType()).ToArray())!;
	}

	public IEnumerable<IMethodParameterInfo> GetParameters()
	{
		return Method.GetParameters().Select(x => new RealMethodParameterInfo(x, TypeFactory, DeclaringRealType));
	}

	public MethodAttributes Attributes => Method.Attributes;
}
