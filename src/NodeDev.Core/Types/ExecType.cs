using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Types
{
	public class ExecType : TypeBase
	{
		public ExecType(TypeFactory typeFactory) : base(typeFactory)
		{
		}

		public override string Name => "Exec";

		public override string FullName => "__Exec__";

		public override bool IsClass => false;

        public override bool IsExec => true;
		
		public override TypeBase[]? Generics => null;

		public override string FriendlyName => "Exec";

        internal override string Serialize()
		{
			return "";
		}

		public static ExecType Deserialize(TypeFactory typeFactory, string serialized)
		{
			return typeFactory.ExecType;
		}

		public override bool IsAssignableTo(TypeBase other)
		{
			throw new NotImplementedException();
		}
	}
}
