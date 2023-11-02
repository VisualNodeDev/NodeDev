﻿using NodeDev.Core.Types;
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

	public RealMethodInfo(TypeFactory typeFactory, MethodInfo method)
	{
		TypeFactory = typeFactory;
		Method = method;
	}

	public string Name => Method.Name;

	public bool IsStatic => Method.IsStatic;

	public TypeBase DeclaringType => TypeFactory.Get(Method.DeclaringType!);

	public TypeBase ReturnType => TypeFactory.Get(Method.ReturnType);

	public IEnumerable<IMethodParameterInfo> GetParameters()
	{
		return Method.GetParameters().Select(x => new RealMethodParameterInfo(x, TypeFactory));
	}

	public class RealMethodParameterInfo : IMethodParameterInfo
	{
		public readonly ParameterInfo ParameterInfo;

		public readonly TypeFactory TypeFactory;

		public RealMethodParameterInfo(ParameterInfo parameterInfo, TypeFactory typeFactory)
		{
			ParameterInfo = parameterInfo;
			TypeFactory = typeFactory;
		}

		public string Name => ParameterInfo.Name ?? "";

		public TypeBase ParameterType => TypeFactory.Get(ParameterInfo.ParameterType);
	}
}