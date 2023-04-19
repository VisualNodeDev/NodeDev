using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Types
{
	public static class TypeFactory
	{
		private static readonly Dictionary<Type, RealType> RealTypes = new();

		public static RealType Get(Type type) => RealTypes.TryGetValue(type, out var realType) ? realType : new(type);

		public static readonly ExecType ExecType = new();
	}
}
