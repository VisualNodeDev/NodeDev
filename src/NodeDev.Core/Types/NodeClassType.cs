using NodeDev.Core.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Types
{
	public class NodeClassType : TypeBase
	{
		public readonly NodeClass NodeClass;

		public NodeClassType(NodeClass nodeClass) : base(nodeClass.Project.TypeFactory)
		{
			NodeClass = nodeClass;
		}

		public override string Name => NodeClass.Name;

		public override string FullName => NodeClass.Namespace + "." + NodeClass.Name;

		public override TypeBase[]? Generics => Array.Empty<TypeBase>(); // not handled yet

		public override string FriendlyName => Name;

		internal override string Serialize()
		{
			return FullName;
		}

		public override bool IsAssignableTo(TypeBase other)
		{
			if (other is RealType realType && realType.BackendType == typeof(object))
				return true;

			// we don't handle node class inheritance yet, therefor a node class can never be assigned to anything
			return false;
		}

		public static NodeClassType Deserialize(TypeFactory typeFactory, string typeName)
		{
			return typeFactory.Get(typeFactory.Project.Classes.First(x => x.Namespace + "." + x.Name == typeName));
		}

		public override IEnumerable<IMethodInfo> GetMethods()
		{
			return NodeClass.Methods;
		}
	}
}
