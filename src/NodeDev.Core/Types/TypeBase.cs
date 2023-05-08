﻿using System;
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

		public virtual bool IsExec => false;

		public virtual bool AllowTextboxEdit => false;

		public virtual string? DefaultTextboxValue => null;

        internal abstract string Serialize();

		public virtual object? ParseTextboxEdit(string text) => throw new NotImplementedException();

		public bool Is<T>() => this == TypeFactory.Get<T>();

		internal static TypeBase Deserialize(string typeFullName, string serializedType)
		{
			var type = TypeFactory.GetTypeByFullName(typeFullName) ?? throw new Exception($"Type not found: {typeFullName}");

			var deserializeMethod = type.GetMethod("Deserialize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) ?? throw new Exception($"Deserialize method not found in type: {typeFullName}");

			var deserializedType = deserializeMethod.Invoke(null, new[] { serializedType });

			if(deserializedType is TypeBase typeBase)
				return typeBase;

			throw new Exception($"Deserialize method in type {typeFullName} returned invalid type");
		}

	}
}
