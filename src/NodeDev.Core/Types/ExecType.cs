using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Types
{
	public class ExecType : TypeBase
	{
		public override string Name => "Exec";

		public override string FullName => "__Exec__";

		public override bool IsClass => false;

        public override bool IsExec => true;
		
		public override TypeBase[] Generics => Array.Empty<TypeBase>();

		public override string FriendlyName => "Exec";

		public override TypeBase? BaseType => throw new NotImplementedException();

		public override TypeBase[] Interfaces => throw new NotImplementedException();

		internal protected override string Serialize()
		{
			return "";
		}

		public new static ExecType Deserialize(TypeFactory typeFactory, string serialized)
		{
			return typeFactory.ExecType;
		}

		public override Type MakeRealType()
		{
			throw new Exception("Unable to make real type with ExecType");
		}

		public override bool IsSameBackend(TypeBase typeBase)
		{
			return typeBase == this;
		}

		//public override bool IsAssignableTo(TypeBase other)
		//{
		//	throw new NotImplementedException();
		//}
		//
		//public override bool IsSame(TypeBase other, bool ignoreGenerics)
		//{
		//	throw new NotImplementedException();
		//}
	}
}
