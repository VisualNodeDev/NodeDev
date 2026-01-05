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
	/// Delegate to check if a node should have a breakpoint set.
	/// This is called during breakpoint setup to filter which nodes should have breakpoints.
	/// Returns true if the node should have a breakpoint, false otherwise.
	/// </summary>
	public Func<string, bool>? ShouldSetBreakpointForNode { get; set; }

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
	/// In the new design, this method is called during module load but only sets breakpoints
	/// for nodes that currently have HasBreakpoint set to true (checked via ShouldSetBreakpointForNode delegate).
	/// </summary>
	/// <param name="nodeFilter">Optional filter to only set breakpoints for specific node IDs. If null, processes all nodes with breakpoints.</param>
	public void TrySetBreakpointsForLoadedModules(Func<NodeBreakpointInfo, bool>? nodeFilter = null)
	{
		if (_breakpointMappings == null || _breakpointMappings.Breakpoints.Count == 0)
			return;
			
		if (_corDebug == null || CurrentProcess == null)
			return;
		
		try
		{
			// Get all app domains
			var appDomains = CurrentProcess.AppDomains.ToArray();
			
			// For each breakpoint mapping, check if we should set a breakpoint
			// Only process nodes that:
			// 1. Pass the filter (if provided), AND
			// 2. Should have a breakpoint (checked via ShouldSetBreakpointForNode delegate)
			var breakpointsToConsider = nodeFilter != null 
				? _breakpointMappings.Breakpoints.Where(nodeFilter)
				: _breakpointMappings.Breakpoints;
			
			// Further filter by ShouldSetBreakpointForNode delegate
			var breakpointsToSet = breakpointsToConsider
				.Where(bp => ShouldSetBreakpointForNode == null || ShouldSetBreakpointForNode(bp.NodeId))
				.ToList();
				
			foreach (var bpInfo in breakpointsToSet)
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
									
									// Look for our project module (project_*)
									if (!moduleName.Contains("project_", StringComparison.OrdinalIgnoreCase))
										continue;
									
									// Found our module! Now try to actually set a breakpoint
									OnDebugCallback(new DebugCallbackEventArgs("BreakpointInfo", 
										$"Found module for breakpoint: {bpInfo.NodeName} in {moduleName}"));
									
									// Try to set an actual breakpoint
									if (TrySetActualBreakpointInModule(module, bpInfo))
									{
										OnDebugCallback(new DebugCallbackEventArgs("BreakpointSet", 
											$"Successfully set breakpoint for {bpInfo.NodeName}"));
									}
									else
									{
										// Still mark as "attempted" to avoid retrying
										if (!_activeBreakpoints.ContainsKey(bpInfo.NodeId))
										{
											_activeBreakpoints[bpInfo.NodeId] = null!;
										}
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
	/// Dynamically sets a breakpoint for a specific node during an active debug session.
	/// This can be called after the process has started to add a breakpoint on-the-fly.
	/// </summary>
	/// <param name="nodeId">The ID of the node to set a breakpoint on.</param>
	/// <returns>True if the breakpoint was set successfully, false otherwise.</returns>
	public bool SetBreakpointForNode(string nodeId)
	{
		if (_breakpointMappings == null)
			return false;
			
		// Find the breakpoint info for this node
		var bpInfo = _breakpointMappings.Breakpoints.FirstOrDefault(bp => bp.NodeId == nodeId);
		if (bpInfo == null)
			return false;
			
		// If already set, return true
		if (_activeBreakpoints.ContainsKey(nodeId))
			return true;
			
		// Set breakpoint for just this node
		TrySetBreakpointsForLoadedModules(bp => bp.NodeId == nodeId);
		
		// Check if it was set successfully
		return _activeBreakpoints.ContainsKey(nodeId);
	}
	
	/// <summary>
	/// Dynamically removes a breakpoint for a specific node during an active debug session.
	/// </summary>
	/// <param name="nodeId">The ID of the node to remove the breakpoint from.</param>
	/// <returns>True if the breakpoint was removed successfully, false if it wasn't set.</returns>
	public bool RemoveBreakpointForNode(string nodeId)
	{
		if (!_activeBreakpoints.TryGetValue(nodeId, out var breakpoint))
			return false;
			
		try
		{
			// Deactivate and dispose the breakpoint
			if (breakpoint != null)
			{
				breakpoint.Activate(false);
				// Note: ClrDebug breakpoints don't have explicit dispose
			}
			
			_activeBreakpoints.Remove(nodeId);
			OnDebugCallback(new DebugCallbackEventArgs("BreakpointRemoved", 
				$"Removed breakpoint for node {nodeId}"));
			return true;
		}
		catch (Exception ex)
		{
			OnDebugCallback(new DebugCallbackEventArgs("BreakpointError", 
				$"Failed to remove breakpoint for node {nodeId}: {ex.Message}"));
			return false;
		}
	}
	
	/// <summary>
	/// Attempts to set an actual ICorDebug breakpoint in a module.
	/// </summary>
	private bool TrySetActualBreakpointInModule(CorDebugModule module, NodeBreakpointInfo bpInfo)
	{
		try
		{
			// Find the function for the method containing this breakpoint
			var function = TryFindFunctionForBreakpoint(module, bpInfo);
			if (function == null)
			{
				OnDebugCallback(new DebugCallbackEventArgs("BreakpointWarning", 
					$"Could not find function for breakpoint in {bpInfo.ClassName}.{bpInfo.MethodName}"));
				return false;
			}
			
			// Get the ICorDebugCode to access IL code
			var code = function.ILCode;
			if (code == null)
			{
				OnDebugCallback(new DebugCallbackEventArgs("BreakpointWarning", 
					$"Could not get IL code for function"));
				return false;
			}
			
			// Check if we have the exact IL offset from the PDB
			if (!bpInfo.ILOffset.HasValue)
			{
				OnDebugCallback(new DebugCallbackEventArgs("BreakpointError", 
					$"No PDB offset available for {bpInfo.SourceFile}:{bpInfo.LineNumber}. Cannot set breakpoint."));
				return false;
			}
			
			uint ilOffset = (uint)bpInfo.ILOffset.Value;
			
			// Create breakpoint at the specific IL offset
			var breakpoint = code.CreateBreakpoint((int)ilOffset);
			breakpoint.Activate(true);
			
			_activeBreakpoints[bpInfo.NodeId] = breakpoint;
			
			OnDebugCallback(new DebugCallbackEventArgs("BreakpointDebug", 
				$"Setting breakpoint at {bpInfo.SourceFile}:{bpInfo.LineNumber}, IL offset {ilOffset}"));
			
			OnDebugCallback(new DebugCallbackEventArgs("BreakpointSet", 
				$"Set breakpoint for node {bpInfo.NodeName} at line {bpInfo.LineNumber}"));
			
			return true;
		}
		catch (Exception ex)
		{
			OnDebugCallback(new DebugCallbackEventArgs("BreakpointError", 
				$"Failed to set breakpoint: {ex.Message}"));
			return false;
		}
	}
	
	/// <summary>
	/// Try to find the function containing the breakpoint
	/// </summary>
	private CorDebugFunction? TryFindFunctionForBreakpoint(CorDebugModule module, NodeBreakpointInfo bpInfo)
	{
		try
		{
			// For now, just find the Main function since we generate simple projects
			// In the future, this should use metadata to find the specific class/method
			return TryFindMainFunction(module);
		}
		catch
		{
			return null;
		}
	}
	
	/// <summary>
	/// Try to find the Main function in a module
	/// </summary>
	private CorDebugFunction? TryFindMainFunction(CorDebugModule module)
	{
		try
		{
			// Try common method tokens for Main
			// In .NET, Main method usually has a specific token range
			// Let's try a range of tokens
			for (uint token = 0x06000001; token < 0x06000100; token++)
			{
				try
				{
					var function = module.GetFunctionFromToken(token);
					if (function != null)
					{
						// We found a function! This might be Main or another method
						// For now, just use the first one we find
						return function;
					}
				}
				catch
				{
					// Token doesn't exist, try next
				}
			}
			
			return null;
		}
		catch
		{
			return null;
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
