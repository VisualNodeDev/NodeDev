using NodeDev.Core.Types;
using System.Text.Json;

namespace NodeDev.Tests;

public class UndefinedGenericTypeTests
{
	[Fact]
	public void SerializeUndefinedGenericType_ReturnsExpectedJson()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));
		var undefinedGenericType = new UndefinedGenericType("T").ArrayType.ArrayType;

		var serialized = undefinedGenericType.Serialize();
		var expectedJson = JsonSerializer.Serialize(new { Name = "T", NbArrayLevels = 2 });

		Assert.Equal(expectedJson, serialized);
	}

	[Fact]
	public void SerializeUndefinedGenericType_Deserialize()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));
		var undefinedGenericType = new UndefinedGenericType("T").ArrayType.ArrayType;

		var serialized = undefinedGenericType.SerializeWithFullTypeName();


		var deserialized = TypeBase.Deserialize(typeFactory, serialized);

		Assert.IsType<UndefinedGenericType>(deserialized);
		Assert.Equal(undefinedGenericType.Name, ((UndefinedGenericType)deserialized).Name);
		Assert.Equal(undefinedGenericType.NbArrayLevels, ((UndefinedGenericType)deserialized).NbArrayLevels);
	}


}