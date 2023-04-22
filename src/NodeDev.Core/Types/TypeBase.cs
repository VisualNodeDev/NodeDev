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

		internal static TypeBase Deserialize(string typeFullName, string serializedType)
		{
			var type = Type.GetType(typeFullName) ?? throw new Exception($"Type not found: {typeFullName}");

			var deserializeMethod = type.GetMethod("Deserialize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) ?? throw new Exception($"Deserialize method not found in type: {typeFullName}");

			var deserializedType = deserializeMethod.Invoke(null, new[] { serializedType });

			if(deserializedType is TypeBase typeBase)
				return typeBase;

			throw new Exception($"Deserialize method in type {typeFullName} returned invalid type");
		}

		internal abstract string Serialize();
	}
}
