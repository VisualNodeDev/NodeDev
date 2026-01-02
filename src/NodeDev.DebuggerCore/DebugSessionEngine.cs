using System.Runtime.InteropServices;
using ClrDebug;

namespace NodeDev.DebuggerCore;

/// <summary>
/// The main debugging engine that manages debug sessions for .NET Core processes.
/// Uses ClrDebug library to interface with the ICorDebug API.
/// </summary>
public class DebugSessionEngine : IDisposable
{
    private readonly string? _dbgShimPath;
    private DbgShim? _dbgShim;
    private IntPtr _dbgShimHandle;
    private bool _disposed;

    /// <summary>
    /// Event raised when a debug callback is received.
    /// </summary>
    public event EventHandler<DebugCallbackEventArgs>? DebugCallback;

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
            // NativeLibrary.Load throws on failure, so no need to check for IntPtr.Zero
            _dbgShimHandle = NativeLibrary.Load(shimPath);
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

        try
        {
            // Initialize the debugging interface
            corDebug.Initialize();

            // Set managed callbacks
            var managedCallback = new ManagedDebuggerCallbacks(this);
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
        }

        if (_dbgShimHandle != IntPtr.Zero)
        {
            NativeLibrary.Free(_dbgShimHandle);
            _dbgShimHandle = IntPtr.Zero;
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
