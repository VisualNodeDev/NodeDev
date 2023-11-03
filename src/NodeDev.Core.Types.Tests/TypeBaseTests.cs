using System.Text.Json;

namespace NodeDev.Core.Types.Tests;

public class TypeBaseTests
{
	[Fact]
	public void Assignations_GetAssignableTypes_Basic()
	{
		var typeFactory = new TypeFactory();

		var type = typeFactory.Get(typeof(List<int>), null);

		var assignables = type.GetAssignableTypes();

		var types = assignables.Select(x => x.Type.MakeRealType()).ToList();

		Assert.Contains(typeof(IList<int>), types);
		Assert.Contains(typeof(ICollection<int>), types);
		Assert.Contains(typeof(IEnumerable<int>), types);
	}

	private class Parent { }
	private class Child : Parent { }

	[Fact]
	public void Assignations_IsAssignableTo_Basic()
	{
		var typeFactory = new TypeFactory();

		var child = typeFactory.Get<Child>();
		var parent = typeFactory.Get<Parent>();
		var childList = typeFactory.Get<List<Child>>();
		var childListList = typeFactory.Get<List<List<Child>>>();
		var parentEnumerable = typeFactory.Get<IEnumerable<Parent>>();
		var parentReadOnlyEnumerable = typeFactory.Get<IReadOnlyList<IEnumerable<Parent>>>();
		var parentListEnumerable = typeFactory.Get<List<IEnumerable<Parent>>>();

		Assert.True(child.IsAssignableTo(parent, out var changedGenerics));
		Assert.Empty(changedGenerics);

		Assert.False(parent.IsAssignableTo(child, out changedGenerics));
		Assert.Null(changedGenerics);

		Assert.True(childList.IsAssignableTo(parentEnumerable, out changedGenerics));
		Assert.Empty(changedGenerics);

		Assert.False(parentEnumerable.IsAssignableTo(childList, out changedGenerics));
		Assert.Null(changedGenerics);

		Assert.True(childListList.IsAssignableTo(parentReadOnlyEnumerable, out changedGenerics));
		Assert.Empty(changedGenerics);

		Assert.False(parentReadOnlyEnumerable.IsAssignableTo(childListList, out changedGenerics));
		Assert.Null(changedGenerics);

		Assert.False(childListList.IsAssignableTo(parentListEnumerable, out changedGenerics));
		Assert.Null(changedGenerics);
	}

	[Fact]
	public void Assignations_IsDirectlyAssignable_InOut()
	{
		var typeFactory = new TypeFactory();

		var childEnumerable = typeFactory.Get(typeof(IEnumerable<>), new[] { typeFactory.Get<Child>() });
		var parentEnumerable = typeFactory.Get(typeof(IEnumerable<>), new[] { typeFactory.Get<Parent>() });
		Assert.True(childEnumerable.IsDirectlyAssignableTo(parentEnumerable, true, out var changedGenerics));
		Assert.Empty(changedGenerics);

		Assert.False(parentEnumerable.IsDirectlyAssignableTo(childEnumerable, true, out changedGenerics));
		Assert.Null(changedGenerics);
	}

	[Fact]
	public void Assignations_IsDirectlyAssignable_Basic()
	{
		var typeFactory = new TypeFactory();

		var type = typeFactory.Get(typeof(List<int>), null);

		Assert.True(type.IsDirectlyAssignableTo(type, true, out var changedGenerics));
		Assert.Empty(changedGenerics);

		Assert.True(type.IsDirectlyAssignableTo(typeFactory.Get(typeof(List<>), new[] { new UndefinedGenericType("T") }), true, out changedGenerics));
		Assert.Single(changedGenerics);
		Assert.Equal("T", changedGenerics.First().Key.Name);
		Assert.Equal(typeof(int), changedGenerics.First().Value.MakeRealType());

		Assert.False(type.IsDirectlyAssignableTo(typeFactory.Get(typeof(IEnumerable<>), new[] { new UndefinedGenericType("T") }), true, out changedGenerics));
		Assert.Null(changedGenerics);

		Assert.True(type.IsDirectlyAssignableTo(new UndefinedGenericType("T"), true, out changedGenerics));
		Assert.Single(changedGenerics);
		Assert.Equal("T", changedGenerics.First().Key.Name);
		Assert.Same(type, changedGenerics.First().Value);

		Assert.True(new UndefinedGenericType("T").IsDirectlyAssignableTo(type, true, out changedGenerics));
		Assert.Single(changedGenerics);
		Assert.Equal("T", changedGenerics.First().Key.Name);
		Assert.Same(type, changedGenerics.First().Value);

		Assert.True(new UndefinedGenericType("T").IsDirectlyAssignableTo(new UndefinedGenericType("T2"), true, out changedGenerics));
		Assert.Empty(changedGenerics);
	}
}