using NodeDev.Core.Types;

namespace NodeDev.Core.Nodes.Delegates;

public static class BclDelegateType
{
	public const int MaximumParameterCount = 16;

	public static TypeBase Create(TypeFactory typeFactory, DelegateKind kind, IReadOnlyList<TypeBase> parameterTypes, TypeBase? resultType)
	{
		ArgumentNullException.ThrowIfNull(typeFactory);
		ArgumentNullException.ThrowIfNull(parameterTypes);

		if (parameterTypes.Count > MaximumParameterCount)
			throw new ArgumentOutOfRangeException(nameof(parameterTypes), $"BCL delegates support at most {MaximumParameterCount} invocation parameters.");

		if (kind == DelegateKind.Action)
		{
			if (resultType != null)
				throw new ArgumentException("Action delegates cannot have a result type.", nameof(resultType));

			if (parameterTypes.Count == 0)
				return typeFactory.Get<Action>();

			var actionType = typeof(Action).Assembly.GetType($"System.Action`{parameterTypes.Count}")
				?? throw new InvalidOperationException($"Unable to resolve System.Action with arity {parameterTypes.Count}.");
			return typeFactory.Get(actionType, parameterTypes.ToArray());
		}

		ArgumentNullException.ThrowIfNull(resultType);
		var genericArguments = parameterTypes.Append(resultType).ToArray();
		var funcType = typeof(Func<>).Assembly.GetType($"System.Func`{genericArguments.Length}")
			?? throw new InvalidOperationException($"Unable to resolve System.Func with arity {genericArguments.Length}.");
		return typeFactory.Get(funcType, genericArguments);
	}

	public static bool TryDescribe(TypeBase type, out DelegateKind kind, out IReadOnlyList<TypeBase> parameterTypes, out TypeBase? resultType)
	{
		kind = default;
		parameterTypes = Array.Empty<TypeBase>();
		resultType = null;

		if (type is not RealType realType)
			return false;

		var backendName = realType.BackendType.FullName;
		if (realType.BackendType == typeof(Action))
		{
			kind = DelegateKind.Action;
			return true;
		}

		if (backendName?.StartsWith("System.Action`", StringComparison.Ordinal) == true && realType.Generics.Length is >= 1 and <= MaximumParameterCount)
		{
			kind = DelegateKind.Action;
			parameterTypes = realType.Generics;
			return true;
		}

		if (backendName?.StartsWith("System.Func`", StringComparison.Ordinal) == true && realType.Generics.Length is >= 1 and <= MaximumParameterCount + 1)
		{
			kind = DelegateKind.Func;
			parameterTypes = realType.Generics[..^1];
			resultType = realType.Generics[^1];
			return true;
		}

		return false;
	}
}
