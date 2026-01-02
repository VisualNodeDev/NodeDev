using ClrDebug;

namespace NodeDev.Core.Debugger;

/// <summary>
/// Implements the ICorDebugManagedCallback interface to receive debugging events.
/// </summary>
internal class ManagedDebuggerCallbacks : ICorDebugManagedCallback, ICorDebugManagedCallback2
{
	private readonly DebugSessionEngine _engine;

	public ManagedDebuggerCallbacks(DebugSessionEngine engine)
	{
		_engine = engine;
	}

	// ICorDebugManagedCallback methods

	public HRESULT Breakpoint(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugBreakpoint pBreakpoint)
	{
		RaiseCallback("Breakpoint", "Breakpoint hit");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT StepComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugStepper pStepper, CorDebugStepReason reason)
	{
		RaiseCallback("StepComplete", $"Step completed: {reason}");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT Break(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread)
	{
		RaiseCallback("Break", "Break occurred");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT Exception(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int unhandled)
	{
		RaiseCallback("Exception", $"Exception occurred (unhandled: {unhandled})");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT EvalComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval pEval)
	{
		RaiseCallback("EvalComplete", "Evaluation completed");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT EvalException(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugEval pEval)
	{
		RaiseCallback("EvalException", "Evaluation threw exception");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT CreateProcess(ICorDebugProcess pProcess)
	{
		RaiseCallback("CreateProcess", "Process created");
		Continue(pProcess);
		return HRESULT.S_OK;
	}

	public HRESULT ExitProcess(ICorDebugProcess pProcess)
	{
		RaiseCallback("ExitProcess", "Process exited");
		// Do not continue after exit
		return HRESULT.S_OK;
	}

	public HRESULT CreateThread(ICorDebugAppDomain pAppDomain, ICorDebugThread thread)
	{
		RaiseCallback("CreateThread", "Thread created");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT ExitThread(ICorDebugAppDomain pAppDomain, ICorDebugThread thread)
	{
		RaiseCallback("ExitThread", "Thread exited");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT LoadModule(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule)
	{
		RaiseCallback("LoadModule", "Module loaded");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT UnloadModule(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule)
	{
		RaiseCallback("UnloadModule", "Module unloaded");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT LoadClass(ICorDebugAppDomain pAppDomain, ICorDebugClass c)
	{
		RaiseCallback("LoadClass", "Class loaded");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT UnloadClass(ICorDebugAppDomain pAppDomain, ICorDebugClass c)
	{
		RaiseCallback("UnloadClass", "Class unloaded");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT DebuggerError(ICorDebugProcess pProcess, HRESULT errorHR, int errorCode)
	{
		RaiseCallback("DebuggerError", $"Debugger error: HR={errorHR}, Code={errorCode}");
		// Continue execution on error
		try { Continue(pProcess); }
		catch { /* Ignore */ }
		return HRESULT.S_OK;
	}

	public HRESULT LogMessage(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, LoggingLevelEnum lLevel, string pLogSwitchName, string pMessage)
	{
		RaiseCallback("LogMessage", $"Log [{lLevel}] {pLogSwitchName}: {pMessage}");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT LogSwitch(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, int lLevel, LogSwitchCallReason ulReason, string pLogSwitchName, string pParentName)
	{
		RaiseCallback("LogSwitch", $"Log switch changed: {pLogSwitchName}");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT CreateAppDomain(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain)
	{
		RaiseCallback("CreateAppDomain", "AppDomain created");
		pAppDomain.Attach();
		Continue(pProcess);
		return HRESULT.S_OK;
	}

	public HRESULT ExitAppDomain(ICorDebugProcess pProcess, ICorDebugAppDomain pAppDomain)
	{
		RaiseCallback("ExitAppDomain", "AppDomain exited");
		Continue(pProcess);
		return HRESULT.S_OK;
	}

	public HRESULT LoadAssembly(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly)
	{
		RaiseCallback("LoadAssembly", "Assembly loaded");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT UnloadAssembly(ICorDebugAppDomain pAppDomain, ICorDebugAssembly pAssembly)
	{
		RaiseCallback("UnloadAssembly", "Assembly unloaded");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT ControlCTrap(ICorDebugProcess pProcess)
	{
		RaiseCallback("ControlCTrap", "Ctrl+C trapped");
		Continue(pProcess);
		return HRESULT.S_OK;
	}

	public HRESULT NameChange(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread)
	{
		RaiseCallback("NameChange", "Name changed");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT UpdateModuleSymbols(ICorDebugAppDomain pAppDomain, ICorDebugModule pModule, IStream pSymbolStream)
	{
		RaiseCallback("UpdateModuleSymbols", "Module symbols updated");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT EditAndContinueRemap(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction, bool fAccurate)
	{
		RaiseCallback("EditAndContinueRemap", "Edit and Continue remap");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT BreakpointSetError(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugBreakpoint pBreakpoint, int dwError)
	{
		RaiseCallback("BreakpointSetError", $"Failed to set breakpoint: error {dwError}");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	// ICorDebugManagedCallback2 methods

	public HRESULT FunctionRemapOpportunity(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pOldFunction, ICorDebugFunction pNewFunction, int oldILOffset)
	{
		RaiseCallback("FunctionRemapOpportunity", "Function remap opportunity");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT CreateConnection(ICorDebugProcess pProcess, int dwConnectionId, string pConnName)
	{
		RaiseCallback("CreateConnection", $"Connection created: {pConnName}");
		Continue(pProcess);
		return HRESULT.S_OK;
	}

	public HRESULT ChangeConnection(ICorDebugProcess pProcess, int dwConnectionId)
	{
		RaiseCallback("ChangeConnection", "Connection changed");
		Continue(pProcess);
		return HRESULT.S_OK;
	}

	public HRESULT DestroyConnection(ICorDebugProcess pProcess, int dwConnectionId)
	{
		RaiseCallback("DestroyConnection", "Connection destroyed");
		Continue(pProcess);
		return HRESULT.S_OK;
	}

	public HRESULT Exception(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFrame pFrame, int nOffset, CorDebugExceptionCallbackType dwEventType, CorDebugExceptionFlags dwFlags)
	{
		RaiseCallback("Exception2", $"Exception event: {dwEventType}");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT ExceptionUnwind(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, CorDebugExceptionUnwindCallbackType dwEventType, CorDebugExceptionFlags dwFlags)
	{
		RaiseCallback("ExceptionUnwind", $"Exception unwind: {dwEventType}");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT FunctionRemapComplete(ICorDebugAppDomain pAppDomain, ICorDebugThread pThread, ICorDebugFunction pFunction)
	{
		RaiseCallback("FunctionRemapComplete", "Function remap complete");
		Continue(pAppDomain);
		return HRESULT.S_OK;
	}

	public HRESULT MDANotification(ICorDebugController pController, ICorDebugThread pThread, ICorDebugMDA pMDA)
	{
		RaiseCallback("MDANotification", "MDA notification");
		Continue(pController);
		return HRESULT.S_OK;
	}

	// Helper methods

	private void RaiseCallback(string callbackType, string description)
	{
		_engine.OnDebugCallback(new DebugCallbackEventArgs(callbackType, description));
	}

	private static void Continue(ICorDebugController controller)
	{
		try
		{
			controller.Continue(false);
		}
		catch
		{
			// Ignore continue errors
		}
	}

	private static void Continue(ICorDebugAppDomain appDomain)
	{
		try
		{
			appDomain.Continue(false);
		}
		catch
		{
			// Ignore continue errors
		}
	}

	private static void Continue(ICorDebugProcess process)
	{
		try
		{
			process.Continue(false);
		}
		catch
		{
			// Ignore continue errors
		}
	}
}
