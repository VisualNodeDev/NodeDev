using System.Text.Json;

namespace NodeDev.Core.Types.Tests;

public class UndefinedGenericTypeTests
{
	[Fact]
	public void SerializeUndefinedGenericType_ReturnsExpectedJson()
	{
		var typeFactory = new TypeFactory();
		var undefinedGenericType = new UndefinedGenericType("T");

		var serialized = undefinedGenericType.Serialize();
		var expectedJson = JsonSerializer.Serialize(new { Name = "T" });

		Assert.Equal(expectedJson, serialized);
	}

	[Fact]
	public void SerializeUndefinedGenericType_Deserialize()
	{
		var typeFactory = new TypeFactory();
		var undefinedGenericType = new UndefinedGenericType("T");

		var serialized = undefinedGenericType.SerializeWithFullTypeName();


		var deserialized = TypeBase.Deserialize(typeFactory, serialized);

		Assert.IsType<UndefinedGenericType>(deserialized);
		Assert.Equal(undefinedGenericType.Name, ((UndefinedGenericType)deserialized).Name);
	}


}