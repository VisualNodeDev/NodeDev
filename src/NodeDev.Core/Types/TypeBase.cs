using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Types
{
	public abstract class TypeBase
	{
		public abstract string Name { get; }

		public abstract string FullName { get; }

		public virtual bool IsClass => true;

		public virtual bool IsGeneric => false;

		public bool IsExec => this == TypeFactory.ExecType;
	}
}
