using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Types;

public class RealType : TypeBase
{
	private readonly Type BackendType;

	public override string Name => BackendType.Name;

	public override string FullName => BackendType.FullName!;

	public override bool IsClass => BackendType.IsClass;

	internal RealType(Type backendType)
	{
		BackendType = backendType;
	}

	internal override string Serialize()
	{
		return FullName;
	}

	public static RealType Deserialize(string fullName)
	{
		var type = Type.GetType(fullName) ?? throw new Exception($"Type not found {fullName}"); ;

		return TypeFactory.Get(type);
	}
}
