using NodeDev.Core.Types;
using System.Text.Json;

namespace NodeDev.Tests;

public class RealTypeTests
{
	[Fact]
	public void Methods_BasicTests()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var type = typeFactory.Get(typeof(Dictionary<,>), new TypeBase[]
		{
			typeFactory.Get(typeof(string), null),
			new UndefinedGenericType("T"),
		});

		var addMethod = type.GetMethods().FirstOrDefault(x => x.Name == "Add");
		Assert.NotNull(addMethod);
		Assert.Equal(2, addMethod.GetParameters().Count());
		Assert.Same(type.Generics[0], addMethod.GetParameters().First().ParameterType);
		Assert.Same(type.Generics[1], addMethod.GetParameters().Last().ParameterType);
		Assert.Same(typeFactory.Get(typeof(void), null), addMethod.ReturnType);
	}

	[Fact]
	public void Constructor_BasicTypeParsing()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var type = typeFactory.Get(typeof(int), null);

		Assert.Same(typeof(int), type.BackendType);

		Assert.Throws<Exception>(() =>
		{
			type = new RealType(typeFactory, typeof(List<>), null); // this should throw an exception since we can't create a RealType with a generic without passing the UndefinedGenericType as argument
		});

		type = typeFactory.Get(typeof(List<int>), null); 
		Assert.Same(typeof(List<>), type.BackendType);
	}

	private class TestBaseClass<T, T2> : Dictionary<int, T> { }
	[Fact]
	public void Constructor_BaseClassGeneric()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var type = typeFactory.Get(typeof(TestBaseClass<,>), new TypeBase[]
		{
			new UndefinedGenericType("T"),
			new RealType(typeFactory, typeof(string), null),
		});

		Assert.NotNull(type.BaseType);
		Assert.Same(typeof(Dictionary<,>), ((RealType)type.BaseType!).BackendType);
		Assert.Same(typeof(int), ((RealType)((RealType)type.BaseType!).Generics[0]).BackendType);
		Assert.Same(type.Generics[0], ((RealType)type.BaseType!).Generics[1]);
	}

	[Fact]
	public void Constructor_InterfaceGeneric()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var type = typeFactory.Get(typeof(Dictionary<,>), new TypeBase[]
		{
			typeFactory.Get(typeof(string), null),
			new UndefinedGenericType("T"),
		});

		Assert.Equal(typeof(Dictionary<,>).GetInterfaces().Length, type.Interfaces.Length);

		var iDictionaryType = type.Interfaces.FirstOrDefault(x => x is RealType realType ? realType.BackendType == typeof(IDictionary<,>) : false);
		Assert.NotNull(iDictionaryType);
		Assert.Same(typeof(string), ((RealType)iDictionaryType.Generics[0]).BackendType);
		Assert.Same(type.Generics[1], iDictionaryType.Generics[1]);
	}

	[Fact]
	public void Constructor_BaseClassNoGeneric()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var type = typeFactory.Get(typeof(int), null);

		Assert.NotNull(type.BaseType);
		Assert.Same(typeof(ValueType), ((RealType)type.BaseType!).BackendType);
	}

	[Fact]
	public void Serialize_Tests()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var type = typeFactory.Get(typeof(Dictionary<,>), new TypeBase[]
		{
			typeFactory.Get(typeof(string), null),
			new UndefinedGenericType("T"),
		});

		var serialized = type.SerializeWithFullTypeName();

		var newTypeFactory = new TypeFactory(new(Guid.NewGuid()));
		var deserialized = (RealType)TypeBase.Deserialize(newTypeFactory, serialized);

		Assert.Same(typeof(Dictionary<,>), deserialized.BackendType);
		Assert.Same(newTypeFactory.Get(typeof(string), null), deserialized.Generics[0]);
		Assert.IsType<UndefinedGenericType>(deserialized.Generics[1]);
		Assert.Equal("T", deserialized.Generics[1].Name);

	}


}