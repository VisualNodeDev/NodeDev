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
			var eventKind = e.Kind;

			// 1. Notify your engine/UI logic
			var description = GetEventDescription(e);
			
			// Handle breakpoint hits specially
			if (eventKind == CorDebugManagedCallbackKind.Breakpoint)
			{
				Console.WriteLine(">>> BREAKPOINT HIT <<<");
				
				// Notify engine about breakpoint hit
				// The engine will figure out which breakpoint was hit based on context
				engine.NotifyBreakpointHit();
				
				engine.OnDebugCallback(new DebugCallbackEventArgs("BreakpointHit", "A breakpoint was hit"));
			}
			
			// Handle module load to set breakpoints
			if (eventKind == CorDebugManagedCallbackKind.LoadModule)
			{
				try
				{
					// Cache the module for later breakpoint setting
					// The event object should have a Module property for LoadModule events
					var moduleProperty = e.GetType().GetProperty("Module");
					if (moduleProperty != null)
					{
						var module = moduleProperty.GetValue(e) as CorDebugModule;
						if (module != null)
						{
							engine.CacheLoadedModule(module);
						}
					}
					
					// Try to set breakpoints when a module loads
					engine.TrySetBreakpointsForLoadedModules();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error setting breakpoints on module load: {ex.Message}");
				}
			}

			engine.OnDebugCallback(new DebugCallbackEventArgs(eventKind.ToString(), description));

			// 2. THE TRAFFIC COP LOGIC
			// We must decide whether to Resume (Continue) or Pause (Wait for User).

			switch (eventKind)
			{
				// --- EVENTS THAT SHOULD PAUSE ---
				case CorDebugManagedCallbackKind.Breakpoint:
				case CorDebugManagedCallbackKind.StepComplete:
				case CorDebugManagedCallbackKind.Exception:
				case CorDebugManagedCallbackKind.Exception2:
				case CorDebugManagedCallbackKind.Break:
					// Do NOT call Continue(). 
					// The process remains suspended so the UI can highlight the node.
					break;

				// --- EVENTS THAT ARE TERMINAL ---
				case CorDebugManagedCallbackKind.ExitProcess:
					// Process is dead, cannot continue.
					break;

				// --- EVENTS THAT SHOULD AUTO-RESUME ---
				default:
					// LoadModule, CreateThread, LogMessage, etc. 
					// These are just setup/noise. We must resume immediately.
					try
					{
						// e.Controller is the ICorDebugController (Process or AppDomain)
						e.Controller.Continue(false);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Failed to auto-continue: {ex.Message}");
					}
					break;
			}
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
