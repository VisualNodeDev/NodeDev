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
}