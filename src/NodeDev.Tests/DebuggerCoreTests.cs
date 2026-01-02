using NodeDev.DebuggerCore;
using Xunit;

namespace NodeDev.Tests;

/// <summary>
/// Unit tests for NodeDev.DebuggerCore.
/// These tests verify the basic functionality of the debug engine components.
/// </summary>
public class DebuggerCoreTests
{
    #region DbgShimResolver Tests

    [Fact]
    public void DbgShimResolver_ShimLibraryName_ShouldReturnPlatformSpecificName()
    {
        // Act
        var name = DbgShimResolver.ShimLibraryName;

        // Assert
        Assert.NotNull(name);
        Assert.NotEmpty(name);

        // Verify it's a platform-specific name
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal("dbgshim.dll", name);
        }
        else if (OperatingSystem.IsLinux())
        {
            Assert.Equal("libdbgshim.so", name);
        }
        else if (OperatingSystem.IsMacOS())
        {
            Assert.Equal("libdbgshim.dylib", name);
        }
    }

    [Fact]
    public void DbgShimResolver_GetAllSearchedPaths_ShouldReturnPaths()
    {
        // Act
        var paths = DbgShimResolver.GetAllSearchedPaths().ToList();

        // Assert
        Assert.NotNull(paths);
        Assert.NotEmpty(paths);

        // All paths should end with the shim library name
        foreach (var (path, _) in paths)
        {
            Assert.EndsWith(DbgShimResolver.ShimLibraryName, path);
        }
    }

    [Fact]
    public void DbgShimResolver_TryResolve_ShouldReturnNullOrValidPath()
    {
        // Act
        var result = DbgShimResolver.TryResolve();

        // Assert
        // Result may be null if dbgshim is not installed (which is common in CI environments)
        // If it's not null, it should be a valid file path
        if (result != null)
        {
            Assert.True(File.Exists(result), $"Resolved path should exist: {result}");
        }
    }

    #endregion

    #region DebugSessionEngine Construction Tests

    [Fact]
    public void DebugSessionEngine_Constructor_ShouldCreateInstance()
    {
        // Act
        using var engine = new DebugSessionEngine();

        // Assert
        Assert.NotNull(engine);
        Assert.False(engine.IsAttached);
        Assert.Null(engine.CurrentProcess);
        Assert.Null(engine.DbgShim);
    }

    [Fact]
    public void DebugSessionEngine_Constructor_WithCustomPath_ShouldStoreValue()
    {
        // Arrange
        const string customPath = "/custom/path/to/dbgshim.so";

        // Act
        using var engine = new DebugSessionEngine(customPath);

        // Assert
        Assert.NotNull(engine);
    }

    [Fact]
    public void DebugSessionEngine_IsAttached_ShouldBeFalseInitially()
    {
        // Arrange
        using var engine = new DebugSessionEngine();

        // Act & Assert
        Assert.False(engine.IsAttached);
    }

    #endregion

    #region DebugSessionEngine Method Tests Without DbgShim

    [Fact]
    public void DebugSessionEngine_Initialize_WithInvalidPath_ShouldThrow()
    {
        // Arrange
        using var engine = new DebugSessionEngine("/nonexistent/path/dbgshim.dll");

        // Act & Assert
        var exception = Assert.Throws<DebugEngineException>(() => engine.Initialize());
        // The message should indicate a failure - different errors may occur depending on the system
        Assert.NotNull(exception.Message);
        Assert.NotEmpty(exception.Message);
    }

    [Fact]
    public void DebugSessionEngine_LaunchProcess_WithoutInitialize_ShouldThrow()
    {
        // Arrange
        using var engine = new DebugSessionEngine();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => engine.LaunchProcess("/some/path.exe"));
    }

    [Fact]
    public void DebugSessionEngine_AttachToProcess_WithoutInitialize_ShouldThrow()
    {
        // Arrange
        using var engine = new DebugSessionEngine();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => engine.AttachToProcess(12345));
    }

    [Fact]
    public void DebugSessionEngine_EnumerateCLRs_WithoutInitialize_ShouldThrow()
    {
        // Arrange
        using var engine = new DebugSessionEngine();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => engine.EnumerateCLRs(12345));
    }

    [Fact]
    public void DebugSessionEngine_Dispose_ShouldNotThrow()
    {
        // Arrange
        var engine = new DebugSessionEngine();

        // Act & Assert - should not throw
        engine.Dispose();
    }

    [Fact]
    public void DebugSessionEngine_DisposeTwice_ShouldNotThrow()
    {
        // Arrange
        var engine = new DebugSessionEngine();

        // Act & Assert - should not throw
        engine.Dispose();
        engine.Dispose();
    }

    [Fact]
    public void DebugSessionEngine_MethodsAfterDispose_ShouldThrowObjectDisposedException()
    {
        // Arrange
        var engine = new DebugSessionEngine();
        engine.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => engine.LaunchProcess("/some/path.exe"));
        Assert.Throws<ObjectDisposedException>(() => engine.AttachToProcess(12345));
        Assert.Throws<ObjectDisposedException>(() => engine.EnumerateCLRs(12345));
    }

    #endregion

    #region DebugCallbackEventArgs Tests

    [Fact]
    public void DebugCallbackEventArgs_Constructor_ShouldSetProperties()
    {
        // Arrange
        const string callbackType = "Breakpoint";
        const string description = "Hit breakpoint at line 42";

        // Act
        var args = new DebugCallbackEventArgs(callbackType, description);

        // Assert
        Assert.Equal(callbackType, args.CallbackType);
        Assert.Equal(description, args.Description);
        Assert.True(args.Continue); // Default should be true
    }

    [Fact]
    public void DebugCallbackEventArgs_Continue_ShouldBeSettable()
    {
        // Arrange
        var args = new DebugCallbackEventArgs("Test", "Test description");

        // Act
        args.Continue = false;

        // Assert
        Assert.False(args.Continue);
    }

    #endregion

    #region LaunchResult Tests

    [Fact]
    public void LaunchResult_Record_ShouldHaveCorrectProperties()
    {
        // Arrange
        const int processId = 12345;
        var resumeHandle = new IntPtr(67890);
        const bool suspended = true;

        // Act
        var result = new LaunchResult(processId, resumeHandle, suspended);

        // Assert
        Assert.Equal(processId, result.ProcessId);
        Assert.Equal(resumeHandle, result.ResumeHandle);
        Assert.Equal(suspended, result.Suspended);
    }

    [Fact]
    public void LaunchResult_Equality_ShouldWork()
    {
        // Arrange
        var result1 = new LaunchResult(123, new IntPtr(456), true);
        var result2 = new LaunchResult(123, new IntPtr(456), true);
        var result3 = new LaunchResult(789, new IntPtr(456), true);

        // Assert
        Assert.Equal(result1, result2);
        Assert.NotEqual(result1, result3);
    }

    #endregion

    #region DebugEngineException Tests

    [Fact]
    public void DebugEngineException_DefaultConstructor_ShouldWork()
    {
        // Act
        var exception = new DebugEngineException();

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public void DebugEngineException_MessageConstructor_ShouldSetMessage()
    {
        // Arrange
        const string message = "Test error message";

        // Act
        var exception = new DebugEngineException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void DebugEngineException_InnerExceptionConstructor_ShouldSetBoth()
    {
        // Arrange
        const string message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new DebugEngineException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    #endregion

    #region Integration Tests (only run if DbgShim is available)

    [Fact]
    public void DebugSessionEngine_Initialize_ShouldSucceedIfDbgShimAvailable()
    {
        // Skip if dbgshim is not available
        var shimPath = DbgShimResolver.TryResolve();
        if (shimPath == null)
        {
            // DbgShim not available - this is expected in many environments
            return;
        }

        // Arrange
        using var engine = new DebugSessionEngine(shimPath);

        // Act - this should succeed
        engine.Initialize();

        // Assert
        Assert.NotNull(engine.DbgShim);
    }

    [Fact]
    public void DebugSessionEngine_LaunchProcess_WithInvalidPath_ShouldThrowArgumentException()
    {
        // Skip if dbgshim is not available
        var shimPath = DbgShimResolver.TryResolve();
        if (shimPath == null)
        {
            return;
        }

        // Arrange
        using var engine = new DebugSessionEngine(shimPath);
        engine.Initialize();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            engine.LaunchProcess("/nonexistent/executable.exe"));
        Assert.Contains("not found", exception.Message);
    }

    #endregion
}
