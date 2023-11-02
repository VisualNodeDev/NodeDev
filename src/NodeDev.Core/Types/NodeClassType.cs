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

		public NodeClassType(NodeClass nodeClass, TypeBase[] generics)
		{
			NodeClass = nodeClass;
			Generics = generics;
		}

		public override string Name => NodeClass.Name;

		public override string FullName => NodeClass.Namespace + "." + NodeClass.Name;

		public override TypeBase[] Generics { get; }

		override public TypeBase? BaseType => null;

		public override string FriendlyName => Name;

		public override IEnumerable<TypeBase> Interfaces => Enumerable.Empty<TypeBase>();

		internal override string Serialize()
		{
			return FullName;
		}

		public override bool IsSame(TypeBase other, bool ignoreGenerics)
		{
			if (other is NodeClassType nodeClassType)
			{
				if (nodeClassType.NodeClass == NodeClass)
				{
					if (ignoreGenerics)
						return true;
					else
					{
						if (Generics.Length != nodeClassType.Generics.Length)
							return false;

						for (int i = 0; i < Generics.Length; i++)
						{
							if (!Generics[i].IsSame(nodeClassType.Generics[i], ignoreGenerics))
								return false;
						}

						return true;
					}
				}
			}

			return false;
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
