using System.Reflection;

namespace NodeDev.ScriptRunner;

/// <summary>
/// ScriptRunner is a console application that serves as the target process for the NodeDev debugger.
/// It loads and executes user-compiled DLLs, providing a separate process for debugging via ICorDebug.
/// </summary>
class Program
{
	static int Main(string[] args)
	{
		// Validate arguments
		if (args.Length == 0)
		{
			Console.Error.WriteLine("Usage: NodeDev.ScriptRunner <path-to-dll> [args...]");
			Console.Error.WriteLine("  <path-to-dll>: Path to the compiled DLL to execute");
			Console.Error.WriteLine("  [args...]: Optional arguments to pass to the entry point");
			return 1;
		}

		string dllPath = args[0];
		string[] userArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();

		// Validate DLL exists
		if (!File.Exists(dllPath))
		{
			Console.Error.WriteLine($"Error: DLL not found at path: {dllPath}");
			return 2;
		}

		try
		{
			// Load the assembly
			Assembly assembly = Assembly.LoadFrom(dllPath);

			// Find and invoke the entry point
			int exitCode = InvokeEntryPoint(assembly, userArgs);

			return exitCode;
		}
		catch (Exception ex)
		{
			// Print exception details - debugger can catch these
			Console.Error.WriteLine($"Fatal error: {ex.GetType().Name}: {ex.Message}");
			Console.Error.WriteLine($"Stack trace:\n{ex.StackTrace}");

			if (ex.InnerException != null)
			{
				Console.Error.WriteLine($"\nInner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
				Console.Error.WriteLine($"Stack trace:\n{ex.InnerException.StackTrace}");
			}

			return 3;
		}
	}

	/// <summary>
	/// Finds and invokes the entry point of the loaded assembly.
	/// Supports: Program.Main static method, or types implementing IRunnable.
	/// </summary>
	private static int InvokeEntryPoint(Assembly assembly, string[] args)
	{
		// Strategy 1: Look for Program.Main static method (in any namespace)
		Type? programType = assembly.GetTypes().FirstOrDefault(t => t.Name == "Program");
		if (programType != null)
		{
			MethodInfo? mainMethod = programType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
			if (mainMethod != null)
			{
				Console.WriteLine($"Invoking {programType.FullName}.Main from {assembly.GetName().Name}");

				object? result = null;
				try
				{
					// Check method signature and invoke appropriately
					ParameterInfo[] parameters = mainMethod.GetParameters();

					if (parameters.Length == 0)
					{
						result = mainMethod.Invoke(null, null);
					}
					else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string[]))
					{
						result = mainMethod.Invoke(null, new object[] { args });
					}
					else
					{
						Console.Error.WriteLine("Warning: Main method has unsupported signature. Invoking with no arguments.");
						result = mainMethod.Invoke(null, null);
					}
				}
				catch (TargetInvocationException tie)
				{
					// Unwrap the real exception from reflection
					if (tie.InnerException != null)
						throw tie.InnerException;
					throw;
				}

				// Convert result to exit code if it's an int
				if (result is int exitCode)
					return exitCode;

				return 0;
			}
		}

		// Strategy 2: Look for types implementing IRunnable
		Type? runnableInterface = assembly.GetType("IRunnable");
		if (runnableInterface != null)
		{
			Type? runnableType = assembly.GetTypes()
				.FirstOrDefault(t => t.GetInterfaces().Contains(runnableInterface));

			if (runnableType != null)
			{
				Console.WriteLine($"Found IRunnable implementation: {runnableType.Name}");

				object? instance = Activator.CreateInstance(runnableType);
				if (instance != null)
				{
					MethodInfo? runMethod = runnableInterface.GetMethod("Run");
					if (runMethod != null)
					{
						try
						{
							Console.WriteLine($"Invoking Run method on {runnableType.Name}");
							runMethod.Invoke(instance, null);
							return 0;
						}
						catch (TargetInvocationException tie)
						{
							if (tie.InnerException != null)
								throw tie.InnerException;
							throw;
						}
					}
				}
			}
		}

		// No entry point found
		Console.Error.WriteLine("Error: No entry point found. Expected:");
		Console.Error.WriteLine("  - Static method Program.Main()");
		Console.Error.WriteLine("  - Type implementing IRunnable interface");
		return 4;
	}
}
