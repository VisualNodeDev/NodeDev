using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Types
{
	public class UndefinedGenericType : TypeBase
	{
		public override string Name { get; }

		public override string FullName { get; }

        public override TypeBase[]? Generics => null;

		public override bool HasUndefinedGenerics => true;

		public override string FriendlyName => Name;

        public UndefinedGenericType(TypeFactory typeFactory, string name) : base(typeFactory)
		{
			FullName = Name = name;
		}

		internal override string Serialize() => Name;

		public static UndefinedGenericType Deserialize(TypeFactory typeFactory, string name) => new(typeFactory, name);

		public override bool IsAssignableTo(TypeBase other)
		{
			throw new NotImplementedException();
		}
	}
}
