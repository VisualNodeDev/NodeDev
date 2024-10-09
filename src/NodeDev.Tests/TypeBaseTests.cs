using NodeDev.Core.Types;
using System.Text.Json;

namespace NodeDev.Tests;

public class TypeBaseTests
{
	[Fact]
	public void Generics_ReplaceUndefinedGeneric()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var t = new UndefinedGenericType("T");
		var type = typeFactory.Get(typeof(List<>), [t]);

		var newType = type.ReplaceUndefinedGeneric(new Dictionary<string, TypeBase>()
		{
			[t.Name] = typeFactory.Get<int>()
		});

		Assert.False(newType.HasUndefinedGenerics);
		Assert.NotSame(type, newType);
		Assert.Same(typeFactory.Get<int>(), newType.Generics[0]);
		Assert.Same(((RealType)newType).BackendType, typeof(List<>));
	}

	[Fact]
	public void Assignations_GetAssignableTypes_Basic()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var type = typeFactory.Get(typeof(List<int>), null);

		var assignableTypes = type.GetAssignableTypes();

		var types = assignableTypes.Select(x => x.Type.MakeRealType()).ToList();

		Assert.Contains(typeof(IList<int>), types);
		Assert.Contains(typeof(ICollection<int>), types);
		Assert.Contains(typeof(IEnumerable<int>), types);
	}

	private class Parent { }
	private class Child : Parent { }

	[Fact]
	public void Assignations_IsAssignableTo_Basic()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var child = typeFactory.Get<Child>();
		var parent = typeFactory.Get<Parent>();
		var childList = typeFactory.Get<List<Child>>();
		var childListList = typeFactory.Get<List<List<Child>>>();
		var parentEnumerable = typeFactory.Get<IEnumerable<Parent>>();
		var parentReadOnlyEnumerable = typeFactory.Get<IReadOnlyList<IEnumerable<Parent>>>();
		var parentListEnumerable = typeFactory.Get<List<IEnumerable<Parent>>>();

		Assert.True(child.IsAssignableTo(parent, out var changedGenericsLeft, out var changedGenericsRight));
		Assert.Empty(changedGenericsLeft);
		Assert.Empty(changedGenericsRight);

		Assert.False(parent.IsAssignableTo(child, out changedGenericsLeft, out changedGenericsRight));
		Assert.Null(changedGenericsLeft);
		Assert.Null(changedGenericsRight);

		Assert.True(childList.IsAssignableTo(parentEnumerable, out changedGenericsLeft, out changedGenericsRight));
        Assert.Empty(changedGenericsLeft);
        Assert.Empty(changedGenericsRight);

        Assert.False(parentEnumerable.IsAssignableTo(childList, out changedGenericsLeft, out changedGenericsRight));
        Assert.Null(changedGenericsLeft);
        Assert.Null(changedGenericsRight);

        Assert.True(childListList.IsAssignableTo(parentReadOnlyEnumerable, out changedGenericsLeft, out changedGenericsRight));
        Assert.Empty(changedGenericsLeft);
        Assert.Empty(changedGenericsRight);

        Assert.False(parentReadOnlyEnumerable.IsAssignableTo(childListList, out changedGenericsLeft, out changedGenericsRight));
        Assert.Null(changedGenericsLeft);
        Assert.Null(changedGenericsRight);

        Assert.False(childListList.IsAssignableTo(parentListEnumerable, out changedGenericsLeft, out changedGenericsRight));
        Assert.Null(changedGenericsLeft);
        Assert.Null(changedGenericsRight);
    }

	[Fact]
	public void Assignations_IsDirectlyAssignable_InOut()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var childEnumerable = typeFactory.Get(typeof(IEnumerable<>), [typeFactory.Get<Child>()]);
		var parentEnumerable = typeFactory.Get(typeof(IEnumerable<>), [typeFactory.Get<Parent>()]);
		Assert.True(childEnumerable.IsDirectlyAssignableTo(parentEnumerable, true, out var changedGenericsLeft, out var changedGenericsRight, out _));
		Assert.Empty(changedGenericsLeft);
		Assert.Empty(changedGenericsRight);

		Assert.False(parentEnumerable.IsDirectlyAssignableTo(childEnumerable, true, out changedGenericsLeft, out changedGenericsRight, out _));
        Assert.Null(changedGenericsLeft);
        Assert.Null(changedGenericsRight);
    }

	[Fact]
	public void Assignations_IsDirectlyAssignable_Basic()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var type = typeFactory.Get(typeof(List<int>), null);

		Assert.True(type.IsDirectlyAssignableTo(type, true, out var changedGenericsLeft, out var changedGenericsRight, out _));
        Assert.Empty(changedGenericsLeft);
        Assert.Empty(changedGenericsRight);

        Assert.True(type.IsDirectlyAssignableTo(typeFactory.Get(typeof(List<>), [new UndefinedGenericType("T")]), true, out changedGenericsLeft, out changedGenericsRight, out _));
        Assert.Empty(changedGenericsLeft);
		Assert.Single(changedGenericsRight);
		Assert.Equal("T", changedGenericsRight.First().Key);
		Assert.Equal(typeof(int), changedGenericsRight.First().Value.MakeRealType());

		Assert.False(type.IsDirectlyAssignableTo(typeFactory.Get(typeof(IEnumerable<>), [new UndefinedGenericType("T")]), true, out changedGenericsLeft, out changedGenericsRight, out _));
        Assert.Null(changedGenericsLeft);
        Assert.Null(changedGenericsRight);

        Assert.True(type.IsDirectlyAssignableTo(new UndefinedGenericType("T"), true, out changedGenericsLeft, out changedGenericsRight, out _));
        Assert.Empty(changedGenericsLeft);
		Assert.Single(changedGenericsRight);
		Assert.Equal("T", changedGenericsRight.First().Key);
		Assert.Same(type, changedGenericsRight.First().Value);

		Assert.True(new UndefinedGenericType("T").IsDirectlyAssignableTo(type, true, out changedGenericsLeft, out changedGenericsRight, out _));
        Assert.Empty(changedGenericsRight);
        Assert.Single(changedGenericsLeft);
        Assert.Equal("T", changedGenericsLeft.First().Key);
        Assert.Same(type, changedGenericsLeft.First().Value);

        Assert.True(new UndefinedGenericType("T").IsDirectlyAssignableTo(new UndefinedGenericType("T2"), true, out changedGenericsLeft, out changedGenericsRight, out _));
		Assert.Empty(changedGenericsLeft);
		Assert.Empty(changedGenericsRight);
	}

	[Fact]
	public void Assignations_IsDirectlyAssignable_BasicArray()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var type = typeFactory.Get(typeof(int), null);       // int
		var typeArr = typeFactory.Get(typeof(int[]), null);  // int[]
		var undefined = new UndefinedGenericType("T");       // T
		var undefinedArr = undefined.ArrayType;              // T[]

		// int[] -> int[]
		Assert.True(typeArr.IsDirectlyAssignableTo(typeArr, true, out var changedGenericsLeft, out var changedGenericsRight, out _));
		Assert.Empty(changedGenericsLeft);
		Assert.Empty(changedGenericsRight);

		// int[] -> int
		Assert.False(typeArr.IsDirectlyAssignableTo(type, true, out _, out _, out _));

		// int -> int[]
		Assert.False(type.IsDirectlyAssignableTo(typeArr, true, out _, out _, out _));

		// int[] -> T
		Assert.True(typeArr.IsDirectlyAssignableTo(undefined, true, out changedGenericsLeft, out changedGenericsRight, out _));
		Assert.Empty(changedGenericsLeft);
		Assert.Single(changedGenericsRight);
        Assert.Equal("T", changedGenericsRight.First().Key);
		Assert.Same(typeArr, changedGenericsRight.First().Value);

		// int[] -> T[]
		Assert.True(typeArr.IsDirectlyAssignableTo(undefinedArr, true, out changedGenericsLeft, out changedGenericsRight, out _));
        Assert.Empty(changedGenericsLeft);
        Assert.Single(changedGenericsRight);
        Assert.Equal("T", changedGenericsRight.First().Key);
		Assert.Same(typeArr.ArrayInnerType, changedGenericsRight.First().Value);

		// T -> int[]
		Assert.True(undefined.IsDirectlyAssignableTo(typeArr, true, out changedGenericsLeft, out changedGenericsRight, out _));
        Assert.Empty(changedGenericsRight);
		Assert.Single(changedGenericsLeft);
        Assert.Equal("T", changedGenericsLeft.First().Key);
		Assert.Same(typeArr, changedGenericsLeft.First().Value);

		// T[] -> int[]
		Assert.True(undefinedArr.IsDirectlyAssignableTo(typeArr, true, out changedGenericsLeft, out changedGenericsRight, out _));
        Assert.Empty(changedGenericsRight);
		Assert.Single(changedGenericsLeft);
		Assert.Equal("T", changedGenericsLeft.First().Key);
		Assert.Same(typeArr.ArrayInnerType, changedGenericsLeft.First().Value);

		// T[] -> T[]
		Assert.True(undefinedArr.IsDirectlyAssignableTo(undefinedArr, true, out changedGenericsLeft, out changedGenericsRight, out _));
		Assert.Empty(changedGenericsLeft);
		Assert.Empty(changedGenericsRight);
    }

	[Fact]
	public void Assignations_IsAssignableTo_BasicArray()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var undefinedType = new UndefinedGenericType("T");                              // T
		var undefinedTypeArr = undefinedType.ArrayType;                                 // T[]
		var undefinedArr = typeFactory.Get(typeof(List<>), [undefinedTypeArr]);         // List<T[]>
		var undefined = typeFactory.Get(typeof(List<>), [undefinedType]);               // List<T>
		var typeArr = typeFactory.Get(typeof(List<>), [typeFactory.Get<int[]>()]);      // List<int[]>
		var type = typeFactory.Get(typeof(List<>), [typeFactory.Get<int>()]);           // List<int>


		// List<int[]> -> List<int[]>
		Assert.True(typeArr.IsAssignableTo(typeArr, out var changedGenericsLeft, out var changedGenericsRight));
		Assert.Empty(changedGenericsLeft);
		Assert.Empty(changedGenericsRight);

        // List<int[]> -> List<int>
        Assert.False(typeArr.IsAssignableTo(type, out _, out _));

		// List<int> -> List<int[]>
		Assert.False(type.IsAssignableTo(typeArr, out _, out _));

		// List<int[]> -> List<T>
		Assert.True(typeArr.IsAssignableTo(undefined, out changedGenericsLeft, out changedGenericsRight));
		Assert.Empty(changedGenericsLeft);
		Assert.Single(changedGenericsRight);
        Assert.Equal("T", changedGenericsRight.First().Key);
		Assert.Same(typeArr.Generics[0], changedGenericsRight.First().Value);

		// List<int[]> -> List<T[]>
		Assert.True(typeArr.IsAssignableTo(undefinedArr, out changedGenericsLeft, out changedGenericsRight));
		Assert.Empty(changedGenericsLeft);
		Assert.Single(changedGenericsRight);
		Assert.Equal("T", changedGenericsRight.First().Key);
		Assert.Same(typeArr.Generics[0].ArrayInnerType, changedGenericsRight.First().Value);

		// List<T> -> List<int[]>
		Assert.True(undefined.IsAssignableTo(typeArr, out changedGenericsLeft, out changedGenericsRight));
		Assert.Empty(changedGenericsRight);
		Assert.Single(changedGenericsLeft);
		Assert.Equal("T", changedGenericsLeft.First().Key);
		Assert.Same(typeArr.Generics[0], changedGenericsLeft.First().Value);

		// List<T[]> -> List<int[]>
		Assert.True(undefinedArr.IsAssignableTo(typeArr, out changedGenericsLeft, out changedGenericsRight));
		Assert.Empty(changedGenericsRight);
		Assert.Single(changedGenericsLeft);
		Assert.Equal("T", changedGenericsLeft.First().Key);
		Assert.Same(typeArr.Generics[0].ArrayInnerType, changedGenericsLeft.First().Value);

		// List<T[]> -> List<T[]>
		Assert.True(undefinedArr.IsAssignableTo(undefinedArr, out changedGenericsLeft, out changedGenericsRight));
		Assert.Empty(changedGenericsLeft);
		Assert.Empty(changedGenericsRight);
	}

	[Fact]
	public void Assignations_IsAssignableTo_ArrayToImplementation()
	{
		var typeFactory = new TypeFactory(new(Guid.NewGuid()));

		var undefinedType = new UndefinedGenericType("T");                                      // T
		var undefinedTypeArr = undefinedType.ArrayType;                                         // T[]
		var undefinedArr = typeFactory.Get(typeof(List<>), [undefinedTypeArr]);                 // List<T[]>
		var undefined = typeFactory.Get(typeof(List<>), [undefinedType]);                       // List<T>
		var typeArr = typeFactory.Get(typeof(List<>), [typeFactory.Get<int[]>()]);              // List<int[]>
		var type = typeFactory.Get(typeof(List<>), [typeFactory.Get<int>()]);                   // List<int>
		var typeImpl = typeFactory.Get(typeof(IEnumerable<>), [typeFactory.Get<int>()]);        // IEnumerable<int>
		var typeImplArr = typeFactory.Get(typeof(IEnumerable<>), [typeFactory.Get<int[]>()]);   // IEnumerable<int[]>
		var undefinedImpl = typeFactory.Get(typeof(IEnumerable<>), [undefinedType]);            // IEnumerable<int>
		var undefinedImplArr = typeFactory.Get(typeof(IEnumerable<>), [undefinedTypeArr]);      // IEnumerable<int[]>


		// List<int[]> -> IEnumerable<int[]>
		Assert.True(typeArr.IsAssignableTo(typeImplArr, out var changedGenericsLeft, out var changedGenericsRight));
		Assert.Empty(changedGenericsLeft);
		Assert.Empty(changedGenericsRight);

        // List<int[]> -> IEnumerable<int>
        Assert.False(typeArr.IsAssignableTo(typeImpl, out _, out _));

		// List<int> -> IEnumerable<int[]>
		Assert.False(type.IsAssignableTo(typeImplArr, out _, out _));

        // IEnumerable<int[]> -> List<int>
        Assert.False(typeImplArr.IsAssignableTo(type, out _, out _));

        // IEnumerable<int> -> List<int[]>
        Assert.False(typeImpl.IsAssignableTo(typeArr, out _, out _));

        // IEnumerable<int[]> -> List<int[]>
        Assert.False(typeImplArr.IsAssignableTo(typeArr, out _, out _));

        // IEnumerable<int> -> List<int>
        Assert.False(typeImpl.IsAssignableTo(type, out _, out _));

        // List<int[]> -> IEnumerable<T>
        Assert.True(typeArr.IsAssignableTo(undefinedImpl, out changedGenericsLeft, out changedGenericsRight));
		Assert.Empty(changedGenericsLeft);
		Assert.Single(changedGenericsRight);
        Assert.Equal("T", changedGenericsRight.First().Key);
		Assert.Same(typeArr.Generics[0], changedGenericsRight.First().Value);

		// List<int[]> -> IEnumerable<T[]>
		Assert.True(typeArr.IsAssignableTo(undefinedImplArr, out changedGenericsLeft, out changedGenericsRight));
		Assert.Empty(changedGenericsLeft);
		Assert.Single(changedGenericsRight);
		Assert.Equal("T", changedGenericsRight.First().Key);
		Assert.Same(typeArr.Generics[0].ArrayInnerType, changedGenericsRight.First().Value);

		// List<T> -> IEnumerable<int[]>
		Assert.True(undefined.IsAssignableTo(typeImplArr, out changedGenericsLeft, out changedGenericsRight));
		Assert.Empty(changedGenericsRight);
		Assert.Single(changedGenericsLeft);
		Assert.Equal("T", changedGenericsLeft.First().Key);
		Assert.Same(typeArr.Generics[0], changedGenericsLeft.First().Value);

		// List<T[]> -> IEnumerable<int[]>
		Assert.True(undefinedArr.IsAssignableTo(typeImplArr, out changedGenericsLeft, out changedGenericsRight));
		Assert.Empty(changedGenericsRight);
		Assert.Single(changedGenericsLeft);
		Assert.Equal("T", changedGenericsLeft.First().Key);
		Assert.Same(typeArr.Generics[0].ArrayInnerType, changedGenericsLeft.First().Value);

		// List<T[]> -> IEnumerable<T[]>
		Assert.True(undefinedArr.IsAssignableTo(undefinedImplArr, out changedGenericsLeft, out changedGenericsRight));
		Assert.Empty(changedGenericsLeft);
		Assert.Empty(changedGenericsRight);
	}
}