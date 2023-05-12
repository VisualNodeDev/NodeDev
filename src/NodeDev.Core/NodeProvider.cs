using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core
{
	public static class NodeProvider
	{
		private static List<Type> NodeTypes = new();
		public static void Initialize()
		{
			AddNodesFromAssembly(typeof(NodeProvider).Assembly);
		}


		// function load a list of all class that inherit from Node
		public static void AddNodesFromAssembly(Assembly assembly)
		{
			var types = assembly.GetTypes().Where(p => typeof(Node).IsAssignableFrom(p) && !p.IsAbstract);

			NodeTypes.AddRange(types);
		}

		public record class NodeSearchResult(Type Type);
		public record class MethodCallNode(Type Type, MethodInfo MethodInfo) : NodeSearchResult(Type);
		public static IEnumerable<NodeSearchResult> Search(string text, Connection? startConnection)
		{
			var nodes = NodeTypes.Where(x => x != typeof(MethodCall)).Where(p => p.Name.Contains(text, StringComparison.OrdinalIgnoreCase));

			var results = nodes.Select(x => new NodeSearchResult(x));

			// check if the text is a method call like 'ClassName.MethodName'
			var methodCallSplit = text.Split('.');
			if (methodCallSplit.Length == 2)
			{
				// try to find the class specified
				TypeFactory.CreateBaseFromUserInput(methodCallSplit[0], out var type);
				if (type != null)
				{
					// find if the method exists
					var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(x => x.Name.Contains(methodCallSplit[1], StringComparison.OrdinalIgnoreCase));

					results = results.Concat(methods.Select(x => new MethodCallNode(typeof(MethodCall), x)));
				}
			}
			else if (startConnection?.Type is RealType realType)
			{
				// find if the method exists
				IEnumerable<MethodInfo> methods = realType.BackendType.GetMethods(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
				// get extensions methods for the realType.BackendType

				methods = methods.Concat(GetExtensionMethods(realType.BackendType)).Where(x => string.IsNullOrWhiteSpace(text) || x.Name.Contains(text, StringComparison.OrdinalIgnoreCase));

				results = results.Concat(methods.Select(x => new MethodCallNode(typeof(MethodCall), x)));
			}

			return results;
		}

		private static IEnumerable<MethodInfo> GetExtensionMethods(Type t)
		{
			var query = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(x => x.GetTypes())
				.Where(type => !type.IsGenericType)
				.SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
				.Where(method => method.IsDefined(typeof(ExtensionAttribute), false) && method.GetParameters()[0].ParameterType.IsAssignableFrom(t));

			return query;
		}
	}
}
