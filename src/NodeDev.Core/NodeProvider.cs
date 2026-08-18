using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Delegates;
using NodeDev.Core.Types;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NodeDev.Core
{
	// this whole way of searching nodes is disgusting.
	// you're welcome
	public static class NodeProvider
	{
		private readonly static List<Type> NodeTypes = [];
		public static void Initialize()
		{
			AddNodesFromAssembly(typeof(NodeProvider).Assembly);
		}

		static NodeProvider()
		{
			Initialize();
		}


		// function load a list of all class that inherit from Node
		public static void AddNodesFromAssembly(Assembly assembly)
		{
			var types = assembly.GetTypes().Where(p => typeof(Node).IsAssignableFrom(p) && !p.IsAbstract);

			NodeTypes.AddRange(types);
		}

		public record class NodeSearchResult(Type Type);
		public record class MethodCallNode(Type Type, IMethodInfo MethodInfo) : NodeSearchResult(Type);
		public record class GetPropertyOrFieldNode(Type Type, IMemberInfo MemberInfo) : NodeSearchResult(Type);
		public record class SetPropertyOrFieldNode(Type Type, IMemberInfo MemberInfo) : NodeSearchResult(Type);
		public record class DelegateCreationNode(Type Type, TypeBase DelegateType) : NodeSearchResult(Type);
		public record class DelegateInvocationNode(Type Type, TypeBase DelegateType) : NodeSearchResult(Type);
		public static IEnumerable<NodeSearchResult> Search(Project project, string text, Connection? startConnection)
			=> SearchCore(project, text, startConnection).Where(x => IsAvailableInScope(x.Type, null, null));

		public static IEnumerable<NodeSearchResult> Search(Graph graph, string text, Connection? startConnection, string? callableScopeId)
		{
			var results = SearchCore(graph.Project, text, startConnection);
			var owner = graph.GetOwningLambda(callableScopeId);

			results = results.Where(result => IsAvailableInScope(result.Type, callableScopeId, owner));

			if (startConnection != null && BclDelegateType.TryDescribe(startConnection.Type, out var kind, out _, out _))
			{
				results = results.Select(result =>
				{
					if (startConnection.IsInput &&
						((kind == DelegateKind.Action && result.Type == typeof(CreateActionNode)) ||
						 (kind == DelegateKind.Func && result.Type == typeof(CreateFuncNode))))
						return (NodeSearchResult)new DelegateCreationNode(result.Type, startConnection.Type);
					if (startConnection.IsOutput && result.Type == typeof(InvokeDelegateNode))
						return new DelegateInvocationNode(result.Type, startConnection.Type);
					return result;
				});
			}

			return results;
		}

		private static bool IsAvailableInScope(Type nodeType, string? callableScopeId, CreateDelegateNode? owner)
		{
			if (nodeType == typeof(LambdaEntryNode))
				return false;
			if (callableScopeId == null)
				return nodeType != typeof(LambdaReturnNode) && nodeType != typeof(LambdaCompleteNode);
			if (owner == null)
				return false;
			if (nodeType == typeof(Nodes.Flow.EntryNode) || nodeType == typeof(Nodes.Flow.ReturnNode))
				return false;
			if (nodeType == typeof(LambdaReturnNode))
				return owner.Kind == DelegateKind.Func;
			if (nodeType == typeof(LambdaCompleteNode))
				return owner.Kind == DelegateKind.Action;
			return true;
		}

		private static IEnumerable<NodeSearchResult> SearchCore(Project project, string text, Connection? startConnection)
		{
			if (startConnection?.Type is UndefinedGenericType)
				startConnection = null; // we want to list every possible choices

			var nodes = NodeTypes.Where(x => x != typeof(MethodCall)).Where(p => p.Name.Contains(text, StringComparison.OrdinalIgnoreCase));

			var results = nodes.Select(x => new NodeSearchResult(x));

			IEnumerable<NodeSearchResult> GetPropertiesAndFields(TypeBase type, string text)
			{
				IEnumerable<IMemberInfo> members = type.GetMembers();
				members = members.Where(x => (x.IsProperty || x.IsField) && x.Name.Contains(text, StringComparison.OrdinalIgnoreCase)); // filter with the name

				IEnumerable<NodeSearchResult> results = members.Where(x => x.CanGet).Select(x => new GetPropertyOrFieldNode(typeof(GetPropertyOrField), x));
				results = results.Concat(members.Where(x => x.CanSet).Select(x => new SetPropertyOrFieldNode(typeof(SetPropertyOrField), x)));

				return results;
			}

			// check if the text is a method call like 'ClassName.MethodName'
			var methodCallSplit = text.Split('.');
			if (methodCallSplit.Length >= 2)
			{
				// try to find the class specified
				project.TypeFactory.CreateBaseFromUserInput(string.Join('.', methodCallSplit[0..^1]), out var type);
				if (type != null)
				{
					// find if the method exists
					var methods = type.GetMethods().Where(x => x.Name.Contains(methodCallSplit[^1], StringComparison.OrdinalIgnoreCase));

					// only keep the methods that are using the startConnection type, if provided
					if (startConnection?.Type?.IsExec == false)
						methods = methods.Where(x => x.GetParameters().Any(y => startConnection.Type.IsAssignableTo(y.ParameterType, out _, out _)));

					results = results.Concat(methods.Select(x => new MethodCallNode(typeof(MethodCall), x)));

					if (startConnection == null)
						results = results.Concat(GetPropertiesAndFields(type, methodCallSplit[1]));
				}
			}
			else if (startConnection?.Type.IsExec == false)
			{
				// find if the method exists
				var methods = startConnection.Type
					.GetMethods()
					.Where(x =>
						!x.Attributes.HasFlag(MethodAttributes.HideBySig) &&  // hide methods such as get/set of properties. Not sure if there's a better way to efficiently do this?
						x.Name.Contains(text, StringComparison.OrdinalIgnoreCase) &&  // search with the text ignoring case
						!x.IsStatic); // Since we're dragging out of a connection, we're expected to only want to execute instance methods

				// get extensions methods for the realType.BackendType
				methods = methods.Concat(GetExtensionMethods(startConnection.Type, project.TypeFactory, text));

				results = results.Concat(methods.Select(x => new MethodCallNode(typeof(MethodCall), x)));

				results = results.Concat(GetPropertiesAndFields(startConnection.Type, text));
			}

			// add methods, get properties and set properties
			results = results.Concat(project.Classes.SelectMany(nodeClass => nodeClass.Methods.Where(x => string.IsNullOrWhiteSpace(text) || x.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).Select(x => new MethodCallNode(typeof(MethodCall), x))));
			results = results.Concat(project.Classes.SelectMany(nodeClass => nodeClass.Properties.Select(x => new GetPropertyOrFieldNode(typeof(GetPropertyOrField), x))));
			results = results.Concat(project.Classes.SelectMany(nodeClass => nodeClass.Properties.Select(x => new SetPropertyOrFieldNode(typeof(SetPropertyOrField), x))));

			// remove any duplicates that may have introduced itself
			results = results.DistinctBy(result =>
			{
				if (result is MethodCallNode methodCallNode)
					return (object)methodCallNode.MethodInfo;
				//if (result is GetPropertyOrFieldNode propertyOrFieldNode)
				//	return (object)propertyOrFieldNode.MemberInfo;
				//if (result is SetPropertyOrFieldNode getPropertyOrFieldNode)
				//	return (object)getPropertyOrFieldNode.MemberInfo;
				return (object)result;
			});

			return results;
		}

		private static readonly ConcurrentDictionary<Assembly, Lazy<List<MethodInfo>>> ExtensionMethodsPerAssembly = [];
		private static readonly ConcurrentDictionary<(Assembly Assembly, Type ReceiverType), ReceiverExtensionMethodCache> ExtensionMethodsPerReceiver = [];
		private static readonly ConditionalWeakTable<Type, TypeShape> TypeShapes = new();
		private static readonly object ExtensionCatalogWarmupLock = new();
		private static Task? ExtensionCatalogWarmupTask;

		private sealed record ExtensionMethodBinding(MethodInfo? Method);

		private sealed class ReceiverExtensionMethodCache(Type receiverType)
		{
			private readonly ConcurrentDictionary<MethodInfo, ExtensionMethodBinding> Bindings = [];

			public MethodInfo? GetOrBind(MethodInfo method)
			{
				return Bindings.GetOrAdd(method, definition => new(TryCloseExtensionMethod(definition, receiverType))).Method;
			}
		}

		private sealed class TypeShape
		{
			private readonly Dictionary<Type, Type[]> ImplementationsByGenericDefinition;

			public TypeShape(Type type)
			{
				ImplementationsByGenericDefinition = GetTypeAndAncestors(type)
					.Where(candidate => candidate.IsGenericType)
					.GroupBy(candidate => candidate.GetGenericTypeDefinition())
					.ToDictionary(group => group.Key, group => group.ToArray());
			}

			public IEnumerable<Type> GetImplementations(Type genericDefinition)
			{
				return ImplementationsByGenericDefinition.TryGetValue(genericDefinition, out var implementations)
					? implementations
					: [];
			}
		}

		public static Task WarmExtensionMethodCatalogAsync()
		{
			lock (ExtensionCatalogWarmupLock)
			{
				return ExtensionCatalogWarmupTask ??= Task.Run(() =>
				{
					foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic))
						_ = GetExtensionMethodsFromAssembly(assembly).Count();
				});
			}
		}

		private static IEnumerable<IMethodInfo> GetExtensionMethods(TypeBase t, TypeFactory typeFactory, string text)
		{
			if (t.HasUndefinedGenerics)
				return [];

			Type receiverType;
			try
			{
				receiverType = t.MakeRealType();
			}
			catch
			{
				return [];
			}

			var hasSearchText = !string.IsNullOrWhiteSpace(text);
			return AppDomain.CurrentDomain.GetAssemblies()
				.Where(assembly => !assembly.IsDynamic)
				.SelectMany(assembly =>
				{
					var receiverCache = ExtensionMethodsPerReceiver.GetOrAdd((assembly, receiverType), _ => new(receiverType));
					return GetExtensionMethodsFromAssembly(assembly)
						.Where(method => !hasSearchText || method.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
						.Select(receiverCache.GetOrBind);
				})
				.Where(method => method != null)
				.Select(method => (IMethodInfo)new RealMethodInfo(typeFactory, method!, typeFactory.Get(method!.DeclaringType!, null)));
		}

		private static IEnumerable<MethodInfo> GetExtensionMethodsFromAssembly(Assembly assembly)
		{
			return ExtensionMethodsPerAssembly
				.GetOrAdd(assembly, currentAssembly => new(
					() => FindExtensionMethodsFromAssembly(currentAssembly),
					LazyThreadSafetyMode.ExecutionAndPublication))
				.Value;
		}

		private static List<MethodInfo> FindExtensionMethodsFromAssembly(Assembly assembly)
		{
			try
			{
				if (!assembly.IsDefined(typeof(ExtensionAttribute), false))
					return [];
			}
			catch
			{
				// If the assembly's custom attributes cannot be inspected, scan its types
				// and let the per-method checks below decide what is usable.
			}

			IEnumerable<Type> types;
			try
			{
				types = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException exception)
			{
				types = exception.Types.OfType<Type>();
			}
			catch
			{
				types = [];
			}

			var methods = new List<MethodInfo>();
			foreach (var type in types.Where(type => type.IsAbstract && type.IsSealed && !type.IsGenericType))
			{
				IEnumerable<MethodInfo> typeMethods;
				try
				{
					typeMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
				}
				catch
				{
					continue;
				}

				foreach (var method in typeMethods)
				{
					try
					{
						if (method.IsDefined(typeof(ExtensionAttribute), false))
							methods.Add(method);
					}
					catch
					{
						// Some tooling assemblies contain attributes whose dependency versions
						// cannot be loaded in the application. They are not usable here anyway.
					}
				}
			}

			return methods;
		}

		private static MethodInfo? TryCloseExtensionMethod(MethodInfo method, Type receiverType)
		{
			ParameterInfo[] parameters;
			try
			{
				parameters = method.GetParameters();
			}
			catch
			{
				return null;
			}
			if (parameters.Length == 0)
				return null;

			if (!method.IsGenericMethodDefinition)
				return IsExtensionReceiverCompatible(parameters[0].ParameterType, receiverType) ? method : null;

			var inferredTypes = new Dictionary<Type, Type>();
			if (!TryInferGenericArguments(parameters[0].ParameterType, receiverType, inferredTypes))
				return null;

			var genericParameters = method.GetGenericArguments();
			if (genericParameters.Any(parameter => !inferredTypes.ContainsKey(parameter)))
				return null;

			try
			{
				var closedMethod = method.MakeGenericMethod(genericParameters.Select(parameter => inferredTypes[parameter]).ToArray());
				return IsExtensionReceiverCompatible(closedMethod.GetParameters()[0].ParameterType, receiverType) ? closedMethod : null;
			}
			catch (ArgumentException)
			{
				return null;
			}
		}

		private static bool TryInferGenericArguments(Type pattern, Type actualType, Dictionary<Type, Type> inferredTypes)
		{
			if (pattern.IsByRef)
				pattern = pattern.GetElementType()!;

			if (pattern.IsGenericMethodParameter)
			{
				if (inferredTypes.TryGetValue(pattern, out var inferredType))
					return inferredType == actualType;

				inferredTypes[pattern] = actualType;
				return true;
			}

			if (pattern.IsArray)
			{
				return actualType.IsArray &&
					pattern.GetArrayRank() == actualType.GetArrayRank() &&
					TryInferGenericArguments(pattern.GetElementType()!, actualType.GetElementType()!, inferredTypes);
			}

			if (!pattern.IsGenericType)
				return pattern.IsAssignableFrom(actualType);

			var patternDefinition = pattern.GetGenericTypeDefinition();
			var matchingTypes = TypeShapes.GetValue(actualType, type => new(type)).GetImplementations(patternDefinition);

			foreach (var matchingType in matchingTypes)
			{
				var candidateInferences = new Dictionary<Type, Type>(inferredTypes);
				var patternArguments = pattern.GetGenericArguments();
				var actualArguments = matchingType.GetGenericArguments();
				if (!patternArguments.Zip(actualArguments).All(pair => TryInferGenericArguments(pair.First, pair.Second, candidateInferences)))
					continue;

				inferredTypes.Clear();
				foreach (var inference in candidateInferences)
					inferredTypes[inference.Key] = inference.Value;
				return true;
			}

			return false;
		}

		private static IEnumerable<Type> GetTypeAndAncestors(Type type)
		{
			yield return type;

			foreach (var @interface in type.GetInterfaces())
				yield return @interface;

			for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
				yield return baseType;
		}

		private static bool IsExtensionReceiverCompatible(Type parameterType, Type receiverType)
		{
			if (parameterType.IsByRef)
				parameterType = parameterType.GetElementType()!;

			return parameterType.IsAssignableFrom(receiverType);
		}
	}
}
