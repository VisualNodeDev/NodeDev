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

		public override bool IsGeneric => true;

		public UndefinedGenericType(string name)
		{
			FullName = Name = name;
		}
	}
}
