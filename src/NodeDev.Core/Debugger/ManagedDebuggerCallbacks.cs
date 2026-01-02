using ClrDebug;

namespace NodeDev.Core.Debugger;

/// <summary>
/// Factory class that creates and configures a CorDebugManagedCallback 
/// with proper event handling for the debug session engine.
/// Uses ClrDebug's built-in CorDebugManagedCallback which handles COM interop correctly.
/// </summary>
public static class ManagedDebuggerCallbackFactory
{
	/// <summary>
	/// Creates a new CorDebugManagedCallback configured to forward events to the debug session engine.
	/// </summary>
	/// <param name="engine">The debug session engine to receive callback events.</param>
	/// <returns>A configured CorDebugManagedCallback instance.</returns>
	public static CorDebugManagedCallback Create(DebugSessionEngine engine)
	{
		var callback = new CorDebugManagedCallback();

		// Subscribe to all events via OnAnyEvent
		callback.OnAnyEvent += (sender, e) =>
		{
			var eventKind = e.Kind.ToString();
			var description = GetEventDescription(e);

			// Log breakpoint hits to console as required
			if (e.Kind == CorDebugManagedCallbackKind.Breakpoint)
			{
				Console.WriteLine("Breakpoint Hit");
			}

			engine.OnDebugCallback(new DebugCallbackEventArgs(eventKind, description));

			// Always continue execution (callbacks auto-continue by default in ClrDebug)
			// e.Controller.Continue(false) is called automatically unless explicitly prevented
		};

		return callback;
	}

	/// <summary>
	/// Gets a human-readable description for a debug callback event.
	/// </summary>
	private static string GetEventDescription(CorDebugManagedCallbackEventArgs e)
	{
		return e.Kind switch
		{
			CorDebugManagedCallbackKind.Breakpoint => "Breakpoint hit",
			CorDebugManagedCallbackKind.StepComplete => "Step completed",
			CorDebugManagedCallbackKind.Break => "Break occurred",
			CorDebugManagedCallbackKind.Exception => "Exception occurred",
			CorDebugManagedCallbackKind.EvalComplete => "Evaluation completed",
			CorDebugManagedCallbackKind.EvalException => "Evaluation threw exception",
			CorDebugManagedCallbackKind.CreateProcess => "Process created",
			CorDebugManagedCallbackKind.ExitProcess => "Process exited",
			CorDebugManagedCallbackKind.CreateThread => "Thread created",
			CorDebugManagedCallbackKind.ExitThread => "Thread exited",
			CorDebugManagedCallbackKind.LoadModule => "Module loaded",
			CorDebugManagedCallbackKind.UnloadModule => "Module unloaded",
			CorDebugManagedCallbackKind.LoadClass => "Class loaded",
			CorDebugManagedCallbackKind.UnloadClass => "Class unloaded",
			CorDebugManagedCallbackKind.DebuggerError => "Debugger error",
			CorDebugManagedCallbackKind.LogMessage => "Log message",
			CorDebugManagedCallbackKind.LogSwitch => "Log switch changed",
			CorDebugManagedCallbackKind.CreateAppDomain => "AppDomain created",
			CorDebugManagedCallbackKind.ExitAppDomain => "AppDomain exited",
			CorDebugManagedCallbackKind.LoadAssembly => "Assembly loaded",
			CorDebugManagedCallbackKind.UnloadAssembly => "Assembly unloaded",
			CorDebugManagedCallbackKind.ControlCTrap => "Ctrl+C trapped",
			CorDebugManagedCallbackKind.NameChange => "Name changed",
			CorDebugManagedCallbackKind.UpdateModuleSymbols => "Module symbols updated",
			CorDebugManagedCallbackKind.EditAndContinueRemap => "Edit and Continue remap",
			CorDebugManagedCallbackKind.BreakpointSetError => "Failed to set breakpoint",
			CorDebugManagedCallbackKind.FunctionRemapOpportunity => "Function remap opportunity",
			CorDebugManagedCallbackKind.CreateConnection => "Connection created",
			CorDebugManagedCallbackKind.ChangeConnection => "Connection changed",
			CorDebugManagedCallbackKind.DestroyConnection => "Connection destroyed",
			CorDebugManagedCallbackKind.Exception2 => "Exception event",
			CorDebugManagedCallbackKind.ExceptionUnwind => "Exception unwind",
			CorDebugManagedCallbackKind.FunctionRemapComplete => "Function remap complete",
			CorDebugManagedCallbackKind.MDANotification => "MDA notification",
			_ => $"Debug event: {e.Kind}"
		};
	}
}
