using System.Text.Json;

namespace NodeDev.Core.Types.Tests;

public class TypeFactoryTests
{
	[Fact]
	public void GetType_NetType()
	{
		var typeFactory = new TypeFactory();

		var type = typeFactory.GetTypeByFullName(typeof(string).FullName!);
		Assert.Same(typeof(string), type);


		type = typeFactory.GetTypeByFullName(typeof(List<int>).FullName!);
		Assert.Same(typeof(List<int>), type);

		type = typeFactory.GetTypeByFullName(typeof(List<>).FullName!);
		Assert.Same(typeof(List<>), type);
	}

	[Fact]
	public void CreateBaseFromUserInputTest()
	{
		var typeFactory = new TypeFactory();

		var err = typeFactory.CreateBaseFromUserInput("int", out var type);
		Assert.Null(err);
		Assert.Equal(typeof(int), type);


		err = typeFactory.CreateBaseFromUserInput("List<int>", out type);
		Assert.Null(err);
		Assert.Equal(typeof(List<int>), type);

		err = typeFactory.CreateBaseFromUserInput("Dictionary<int, string>", out type);
		Assert.Null(err);
		Assert.Equal(typeof(Dictionary<int, string>), type);
	}
}