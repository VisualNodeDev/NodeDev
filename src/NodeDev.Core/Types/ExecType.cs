﻿namespace NodeDev.Core.Types
{
	public class ExecType : TypeBase
	{
		public override string Name => "Exec";

		public override string FullName => "__Exec__";

		public override bool IsClass => false;

		public override bool IsExec => true;

		public override TypeBase[] Generics => [];

		public override string FriendlyName => "Exec";

		public override TypeBase? BaseType => throw new NotImplementedException();

		public override TypeBase[] Interfaces => throw new NotImplementedException();

		public override bool IsArray => false;

		public override TypeBase ArrayInnerType => throw new Exception("Can't call ArrayInnerType on ExecType");

		public override TypeBase ArrayType => throw new Exception("Can't call ArrayType on ExecType");

		public override IEnumerable<IMemberInfo> GetMembers() => throw new NotImplementedException();

		public override IEnumerable<IMethodInfo> GetMethods() => [];

		public override IEnumerable<IMethodInfo> GetMethods(string name) => [];

		public override TypeBase CloneWithGenerics(TypeBase[] newGenerics)
		{
			if (newGenerics.Length != 0)
				throw new Exception("ExecType does not have generics");

			return this;
		}

		internal protected override string Serialize()
		{
			return "";
		}

		public static ExecType Deserialize(TypeFactory typeFactory, string serialized)
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
