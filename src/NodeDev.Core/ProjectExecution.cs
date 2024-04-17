
namespace NodeDev.Core;

public static class ProjectExecution
{
	private static readonly List<Delegate?> ClassMethods = new(500);

	/// <summary>
	/// Create a unique id for the class method.
	/// The id can later on be used to register the method delegate.
	/// </summary>
	/// <returns>unique id</returns>
	internal static int PreRegisterProjectClassMethod()
	{
		var id = ClassMethods.Count;
		ClassMethods.Add(null);

		return id;
	}

	/// <summary>
	/// Add the method delegate to the list of class methods.
	/// </summary>
	/// <param name="id">Id of the method to link with the delegate.</param>
	/// <param name="method">delegate that will be retrieved later on.</param>
	internal static void RegisterProjectClassMethod(int id, Delegate method)
	{
		ClassMethods[id] = method;
	}

	/// <summary>
	/// Get the method delegate from the list of class methods.
	/// </summary>
	/// <param name="id">Id of the method.</param>
	/// <returns>The method delegate</returns>
	public static Delegate GetMethodGraphDelegate(int id)
	{
		return ClassMethods[id]!;
	}

}
