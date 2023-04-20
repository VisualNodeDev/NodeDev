using NodeDev.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
			var types = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(s => s.GetTypes())
				.Where(p => typeof(Node).IsAssignableFrom(p) && !p.IsAbstract);

			NodeTypes.AddRange(types);
		}
	}
}
