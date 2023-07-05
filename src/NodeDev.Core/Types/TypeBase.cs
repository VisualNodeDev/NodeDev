using NodeDev.Core.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Types
{
	public abstract class TypeBase
	{
		public abstract string Name { get; }

		public abstract string FullName { get; }

		public virtual bool IsClass => true;

		public virtual bool HasUndefinedGenerics => Generics?.Any(x => x is UndefinedGenericType) ?? false;

		public abstract TypeBase[]? Generics { get; }

		public virtual bool IsExec => false;

		public virtual bool AllowTextboxEdit => false;

		public virtual string? DefaultTextboxValue => null;

		public abstract string FriendlyName { get; }

        internal abstract string Serialize();

		public virtual object? ParseTextboxEdit(string text) => throw new NotImplementedException();

		public abstract bool IsAssignableTo(TypeBase other);

		public virtual IEnumerable<IMethodInfo> GetMethods() => Enumerable.Empty<IMethodInfo>();

		public readonly TypeFactory TypeFactory;

		public TypeBase(TypeFactory typeFactory)
		{
			TypeFactory = typeFactory;
		}

		internal static TypeBase Deserialize(TypeFactory typeFactory, string typeFullName, string serializedType)
		{
			var type = typeFactory.GetTypeByFullName(typeFullName) ?? throw new Exception($"Type not found: {typeFullName}");

			var deserializeMethod = type.GetMethod("Deserialize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) ?? throw new Exception($"Deserialize method not found in type: {typeFullName}");

			var deserializedType = deserializeMethod.Invoke(null, new object[] { typeFactory, serializedType });

			if(deserializedType is TypeBase typeBase)
				return typeBase;

			throw new Exception($"Deserialize method in type {typeFullName} returned invalid type");
		}

	}
}
