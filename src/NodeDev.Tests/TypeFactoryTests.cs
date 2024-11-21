using NodeDev.Core.Types;

namespace NodeDev.Tests;

public class TypeFactoryTests
{
	[Fact]
	public void GetType_NetType()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

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
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var err = typeFactory.CreateBaseFromUserInput("int", out var type);
		Assert.Null(err);
		Assert.Equal(typeFactory.Get<int>(), type);


		err = typeFactory.CreateBaseFromUserInput("List<int>", out type);
		Assert.Null(err);
		Assert.IsType<RealType>(type);
		Assert.Same(typeof(List<>), ((RealType)type).BackendType);
		Assert.Same(typeof(int), ((RealType)((RealType)type).Generics[0]).BackendType);

		err = typeFactory.CreateBaseFromUserInput("Dictionary<int, string>", out type);
		Assert.Null(err);
		Assert.IsType<RealType>(type);
		Assert.Same(typeof(Dictionary<,>), ((RealType)type).BackendType);
		Assert.Same(typeof(int), ((RealType)((RealType)type).Generics[0]).BackendType);
		Assert.Same(typeof(string), ((RealType)((RealType)type).Generics[1]).BackendType);
	}

	[Fact]
	public void CreateBaseFromUserInputTest_Err()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var err = typeFactory.CreateBaseFromUserInput("asdfasdf", out var type);
		Assert.NotNull(err);
		Assert.Null(type);

		err = typeFactory.CreateBaseFromUserInput("Dictionary", out type);
		Assert.NotNull(err);
		Assert.Null(type);
	}
}