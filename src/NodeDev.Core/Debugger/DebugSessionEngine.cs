using ClrDebug;
using System.Runtime.InteropServices;

namespace NodeDev.Core.Debugger;

/// <summary>
/// The main debugging engine that manages debug sessions for .NET Core processes.
/// Uses ClrDebug library to interface with the ICorDebug API.
/// </summary>
public class DebugSessionEngine : IDisposable
{
	private readonly string? _dbgShimPath;
	private DbgShim? _dbgShim;
	private static IntPtr _dbgShimHandle; // Now static
	private bool _disposed;
	private readonly Dictionary<string, CorDebugFunctionBreakpoint> _activeBreakpoints = new();
	private BreakpointMappingInfo? _breakpointMappings;
	private CorDebug? _corDebug;

	/// <summary>
	/// Event raised when a debug callback is received.
	/// </summary>
	public event EventHandler<DebugCallbackEventArgs>? DebugCallback;
	
	/// <summary>
	/// Event raised when a breakpoint is hit.
	/// Provides the NodeBreakpointInfo for the node where the breakpoint was hit.
	/// </summary>
	public event EventHandler<NodeBreakpointInfo>? BreakpointHit;

	/// <summary>
	/// Gets the current debug process, if any.
	/// </summary>
	public CorDebugProcess? CurrentProcess { get; private set; }

	/// <summary>
	/// Gets a value indicating whether the debugger is currently attached to a process.
	/// </summary>
	public bool IsAttached => CurrentProcess != null;

	/// <summary>
	/// Gets the DbgShim instance used by this engine.
	/// </summary>
	public DbgShim? DbgShim => _dbgShim;

	/// <summary>
	/// Initializes a new instance of the DebugSessionEngine class.
	/// </summary>
	/// <param name="dbgShimPath">Optional path to the dbgshim library. If null, auto-detection is used.</param>
	public DebugSessionEngine(string? dbgShimPath = null)
	{
		_dbgShimPath = dbgShimPath;
	}

	/// <summary>
	/// Initializes the debug engine by loading the dbgshim library.
	/// </summary>
	/// <exception cref="DebugEngineException">Thrown when initialization fails.</exception>
	public void Initialize()
	{
		if (_dbgShim != null)
			return;

		try
		{
			var shimPath = _dbgShimPath ?? DbgShimResolver.Resolve();
			// Only load the library once globally
			if (_dbgShimHandle == IntPtr.Zero)
			{
				_dbgShimHandle = NativeLibrary.Load(shimPath);
			}
			_dbgShim = new DbgShim(_dbgShimHandle);
		}
		catch (FileNotFoundException ex)
		{
			throw new DebugEngineException("DbgShim library not found. Debug support may not be installed.", ex);
		}
		catch (DllNotFoundException ex)
		{
			throw new DebugEngineException($"Failed to load dbgshim library: {ex.Message}", ex);
		}
		catch (Exception ex) when (ex is not DebugEngineException)
		{
			throw new DebugEngineException($"Failed to initialize debug engine: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Launches a process for debugging and attaches to it.
	/// </summary>
	/// <param name="executablePath">Path to the executable to debug.</param>
	/// <param name="arguments">Command-line arguments for the executable.</param>
	/// <param name="workingDirectory">Working directory for the process. If null, uses the executable's directory.</param>
	/// <returns>Information about the launched process.</returns>
	public LaunchResult LaunchProcess(string executablePath, string? arguments = null, string? workingDirectory = null)
	{
		ThrowIfDisposed();
		EnsureInitialized();

		if (!File.Exists(executablePath))
		{
			throw new ArgumentException($"Executable not found: {executablePath}", nameof(executablePath));
		}

		var commandLine = arguments != null
			? $"\"{executablePath}\" {arguments}"
			: $"\"{executablePath}\"";

		workingDirectory ??= Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory;

		try
		{
			// Create process suspended so we can set up debugging before it starts
			var process = _dbgShim!.CreateProcessForLaunch(commandLine, bSuspendProcess: true);

			return new LaunchResult(
				ProcessId: process.ProcessId,
				ResumeHandle: process.ResumeHandle,
				Suspended: true
			);
		}
		catch (Exception ex)
		{
			throw new DebugEngineException($"Failed to launch process: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Resumes a suspended process that was launched for debugging.
	/// </summary>
	/// <param name="resumeHandle">The resume handle from LaunchProcess.</param>
	public void ResumeProcess(IntPtr resumeHandle)
	{
		ThrowIfDisposed();
		EnsureInitialized();

		try
		{
			_dbgShim!.ResumeProcess(resumeHandle);
		}
		catch (Exception ex)
		{
			throw new DebugEngineException($"Failed to resume process: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Closes the resume handle after the process has been resumed.
	/// </summary>
	/// <param name="resumeHandle">The resume handle to close.</param>
	public void CloseResumeHandle(IntPtr resumeHandle)
	{
		ThrowIfDisposed();
		EnsureInitialized();

		try
		{
			_dbgShim!.CloseResumeHandle(resumeHandle);
		}
		catch (Exception ex)
		{
			throw new DebugEngineException($"Failed to close resume handle: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Registers for runtime startup notification for a given process.
	/// This allows the debugger to be notified when the CLR is loaded.
	/// </summary>
	/// <param name="processId">The process ID to monitor.</param>
	/// <param name="callback">Callback to invoke when the runtime starts with the raw ICorDebug pointer.</param>
	/// <returns>An unregister token that must be passed to UnregisterForRuntimeStartup.</returns>
	public IntPtr RegisterForRuntimeStartup(int processId, Action<IntPtr, HRESULT> callback)
	{
		ThrowIfDisposed();
		EnsureInitialized();

		try
		{
			var token = _dbgShim!.RegisterForRuntimeStartup(
				processId,
				(pCordb, parameter, hr) => callback(pCordb, hr),
				IntPtr.Zero
			);

			return token;
		}
		catch (Exception ex)
		{
			throw new DebugEngineException($"Failed to register for runtime startup: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Unregisters from runtime startup notification.
	/// </summary>
	/// <param name="token">The token returned from RegisterForRuntimeStartup.</param>
	public void UnregisterForRuntimeStartup(IntPtr token)
	{
		ThrowIfDisposed();
		EnsureInitialized();

		try
		{
			_dbgShim!.UnregisterForRuntimeStartup(token);
		}
		catch (Exception ex)
		{
			throw new DebugEngineException($"Failed to unregister for runtime startup: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Enumerates the CLR instances loaded in a process.
	/// </summary>
	/// <param name="processId">The process ID to enumerate.</param>
	/// <returns>Array of runtime module paths.</returns>
	public string[] EnumerateCLRs(int processId)
	{
		ThrowIfDisposed();
		EnsureInitialized();

		try
		{
			// Check if process is still running before attempting CLR enumeration
			var process = System.Diagnostics.Process.GetProcessById(processId);
			if (process.HasExited)
			{
				throw new DebugEngineException($"Cannot enumerate CLRs: Target process {processId} has already exited.");
			}

			var result = _dbgShim!.EnumerateCLRs(processId);
			return result.Items.Select(item => item.Path).ToArray();
		}
		catch (Exception ex)
		{
			throw new DebugEngineException($"Failed to enumerate CLRs: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Attaches to an already running process for debugging.
	/// </summary>
	/// <param name="processId">The process ID to attach to.</param>
	/// <returns>The CorDebug interface for the attached process.</returns>
	public CorDebug AttachToProcess(int processId)
	{
		ThrowIfDisposed();
		EnsureInitialized();

		try
		{
			// First, enumerate CLRs to make sure there's a runtime loaded
			var clrs = EnumerateCLRs(processId);
			if (clrs.Length == 0)
			{
				throw new DebugEngineException(
					"No CLR found in the target process. The process may not be a .NET process or the CLR has not been loaded yet.");
			}

			// Get the version string for the first CLR
			var versionString = _dbgShim!.CreateVersionStringFromModule(processId, clrs[0]);

			// Create the debugging interface (returns CorDebug wrapper directly)
			var corDebug = _dbgShim.CreateDebuggingInterfaceFromVersionEx(CorDebugInterfaceVersion.CorDebugLatestVersion, versionString);

			return corDebug;
		}
		catch (Exception ex) when (ex is not DebugEngineException)
		{
			throw new DebugEngineException($"Failed to attach to process {processId}: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Sets up debugging for a process, including initializing the CorDebug interface and setting up callbacks.
	/// </summary>
	/// <param name="corDebug">The CorDebug interface.</param>
	/// <param name="processId">The process ID to debug.</param>
	/// <returns>The CorDebugProcess representing the debugged process.</returns>
	public CorDebugProcess SetupDebugging(CorDebug corDebug, int processId)
	{
		ThrowIfDisposed();
		
		// Store CorDebug instance for later use
		_corDebug = corDebug;

		try
		{
			// Initialize the debugging interface
			corDebug.Initialize();

			// Set managed callbacks using ClrDebug's CorDebugManagedCallback
			var managedCallback = ManagedDebuggerCallbackFactory.Create(this);
			corDebug.SetManagedHandler(managedCallback);

			// Attach to the process
			var process = corDebug.DebugActiveProcess(processId, win32Attach: false);
			CurrentProcess = process;

			return process;
		}
		catch (Exception ex)
		{
			throw new DebugEngineException($"Failed to set up debugging for process {processId}: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Detaches from the current debug process.
	/// </summary>
	public void Detach()
	{
		if (CurrentProcess != null)
		{
			try
			{
				CurrentProcess.Stop(0);
				CurrentProcess.Detach();
			}
			catch
			{
				// Ignore errors during detach
			}
			finally
			{
				CurrentProcess = null;
			}
		}
	}
	
	/// <summary>
	/// Continues execution after a breakpoint or other pause event.
	/// </summary>
	public void Continue()
	{
		if (CurrentProcess == null)
			throw new InvalidOperationException("No process is currently being debugged.");
		
		try
		{
			CurrentProcess.Continue(false);
			OnDebugCallback(new DebugCallbackEventArgs("Continue", "Execution resumed"));
		}
		catch (Exception ex)
		{
			throw new DebugEngineException($"Failed to continue execution: {ex.Message}", ex);
		}
	}
	
	/// <summary>
	/// Sets the breakpoint mappings for the current debug session.
	/// This must be called before attaching to set breakpoints after modules load.
	/// </summary>
	/// <param name="mappings">The breakpoint mapping information from compilation.</param>
	public void SetBreakpointMappings(BreakpointMappingInfo? mappings)
	{
		_breakpointMappings = mappings;
	}
	
	/// <summary>
	/// Sets breakpoints in the debugged process based on the breakpoint mappings.
	/// This should be called after modules are loaded (typically in LoadModule callback).
	/// </summary>
	public void TrySetBreakpointsForLoadedModules()
	{
		if (_breakpointMappings == null || _breakpointMappings.Breakpoints.Count == 0)
			return;
			
		if (_corDebug == null || CurrentProcess == null)
			return;
		
		try
		{
			// Get all app domains
			var appDomains = CurrentProcess.AppDomains.ToArray();
			
			// For each breakpoint mapping, try to set a breakpoint
			foreach (var bpInfo in _breakpointMappings.Breakpoints)
			{
				// Skip if already set
				if (_activeBreakpoints.ContainsKey(bpInfo.NodeId))
					continue;
					
				try
				{
					// Try to set breakpoint in each app domain
					foreach (var appDomain in appDomains)
					{
						// Enumerate assemblies in the app domain
						var assemblies = appDomain.Assemblies.ToArray();
						
						foreach (var assembly in assemblies)
						{
							// Enumerate modules in the assembly
							var modules = assembly.Modules.ToArray();
							
							// Find the module containing our generated code
							foreach (var module in modules)
							{
								try
								{
									var moduleName = module.Name;
									// Look for our project module (NodeProject_*)
									if (!moduleName.Contains("NodeProject_", StringComparison.OrdinalIgnoreCase))
										continue;
									
									// For now, just log that we found the module
									// Actually setting breakpoints using ClrDebug requires complex metadata parsing
									// which we'll implement in a follow-up
									OnDebugCallback(new DebugCallbackEventArgs("BreakpointInfo", 
										$"Found module for breakpoint: {bpInfo.NodeName} in {moduleName}"));
									
									// Mark as "set" so we don't keep trying
									if (!_activeBreakpoints.ContainsKey(bpInfo.NodeId))
									{
										// Create a dummy entry to prevent retrying
										// In a real implementation, this would be the actual breakpoint
										_activeBreakpoints[bpInfo.NodeId] = null!;
									}
								}
								catch (Exception ex)
								{
									OnDebugCallback(new DebugCallbackEventArgs("BreakpointWarning", 
										$"Failed to check module: {ex.Message}"));
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					OnDebugCallback(new DebugCallbackEventArgs("BreakpointError", 
						$"Failed to set breakpoint for node '{bpInfo.NodeName}': {ex.Message}"));
				}
			}
		}
		catch (Exception ex)
		{
			OnDebugCallback(new DebugCallbackEventArgs("BreakpointError", 
				$"Failed to set breakpoints: {ex.Message}"));
		}
	}
	
	/// <summary>
	/// Handles a breakpoint hit event.
	/// Maps the breakpoint back to the node that triggered it.
	/// </summary>
	/// <param name="breakpoint">The breakpoint that was hit.</param>
	internal void OnBreakpointHit(CorDebugFunctionBreakpoint breakpoint)
	{
		// Find which node this breakpoint corresponds to
		var nodeId = _activeBreakpoints.FirstOrDefault(kvp => kvp.Value == breakpoint).Key;
		
		if (nodeId != null && _breakpointMappings != null)
		{
			var bpInfo = _breakpointMappings.Breakpoints.FirstOrDefault(b => b.NodeId == nodeId);
			if (bpInfo != null)
			{
				BreakpointHit?.Invoke(this, bpInfo);
				OnDebugCallback(new DebugCallbackEventArgs("BreakpointHit", 
					$"Breakpoint hit: Node '{bpInfo.NodeName}' in {bpInfo.ClassName}.{bpInfo.MethodName}"));
			}
		}
	}

	/// <summary>
	/// Invokes the DebugCallback event.
	/// </summary>
	internal void OnDebugCallback(DebugCallbackEventArgs args)
	{
		DebugCallback?.Invoke(this, args);
	}

	private void EnsureInitialized()
	{
		if (_dbgShim == null)
		{
			throw new InvalidOperationException("Debug engine not initialized. Call Initialize() first.");
		}
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(nameof(DebugSessionEngine));
		}
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Disposes of the debug engine resources.
	/// </summary>
	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
			return;

		if (disposing)
		{
			Detach();
			// Do NOT free _dbgShimHandle here, since it's static/global and shared
		}

		_dbgShim = null;
		_disposed = true;
	}

	/// <summary>
	/// Finalizer.
	/// </summary>
	~DebugSessionEngine()
	{
		Dispose(false);
	}
}

/// <summary>
/// Result of launching a process for debugging.
/// </summary>
/// <param name="ProcessId">The process ID of the launched process.</param>
/// <param name="ResumeHandle">Handle to use when resuming the process.</param>
/// <param name="Suspended">Whether the process is currently suspended.</param>
public record LaunchResult(int ProcessId, IntPtr ResumeHandle, bool Suspended);

/// <summary>
/// Event arguments for debug callbacks.
/// </summary>
public class DebugCallbackEventArgs : EventArgs
{
	/// <summary>
	/// Gets the type of callback.
	/// </summary>
	public string CallbackType { get; }

	/// <summary>
	/// Gets a description of the callback.
	/// </summary>
	public string Description { get; }

	/// <summary>
	/// Gets or sets whether to continue execution after handling this callback.
	/// </summary>
	public bool Continue { get; set; } = true;

	/// <summary>
	/// Initializes a new instance of DebugCallbackEventArgs.
	/// </summary>
	public DebugCallbackEventArgs(string callbackType, string description)
	{
		CallbackType = callbackType;
		Description = description;
	}
}
