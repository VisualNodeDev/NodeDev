using System.Reflection;

namespace NodeDev.Core.Types;

public class RealMemberInfo : IMemberInfo
{
	internal readonly MemberInfo MemberInfo;
	private readonly TypeFactory TypeFactory;
	private readonly RealType RealType;

	public RealMemberInfo(MemberInfo memberInfo, RealType realType, TypeFactory typeFactory)
	{
		MemberInfo = memberInfo;
		TypeFactory = typeFactory;
		RealType = realType;
	}

	public TypeBase DeclaringType => RealType;

	public string Name => MemberInfo.Name;

	public TypeBase MemberType
	{
		get
		{
			var t = MemberInfo switch
			{
				FieldInfo field => field.FieldType,
				PropertyInfo property => property.PropertyType,
				_ => throw new Exception("Invalid member type")
			};
			if (t.IsGenericParameter)
				return RealType.Generics[t.GenericParameterPosition];

			return TypeFactory.Get(t, null);
		}
	}

	public bool IsStatic => MemberInfo switch
	{
		FieldInfo field => field.IsStatic,
		PropertyInfo property => property.GetMethod?.IsStatic ?? false,
		_ => throw new Exception("Invalid member type")
	};
}
