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
		private readonly NodeClass NodeClass;

		public NodeClassType(NodeClass nodeClass)
		{
			NodeClass = nodeClass;
		}

		public override string Name => NodeClass.Name;

		public override string FullName => NodeClass.Namespace + "." + NodeClass.Name;

		public override TypeBase[]? Generics => Array.Empty<TypeBase>(); // not handled yet

		public override string FriendlyName => throw new NotImplementedException();

		internal override string Serialize()
		{
			return FullName;
		}
	}
}
