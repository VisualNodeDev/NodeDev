using System.Runtime.InteropServices;

namespace NodeDev.DebuggerCore;

/// <summary>
/// Resolves the path to the dbgshim library (dbgshim.dll on Windows, libdbgshim.so on Linux).
/// DbgShim is required to debug .NET Core applications via the ICorDebug API.
/// </summary>
public static class DbgShimResolver
{
    private const string WindowsShimName = "dbgshim.dll";
    private const string LinuxShimName = "libdbgshim.so";
    private const string MacOSShimName = "libdbgshim.dylib";

    /// <summary>
    /// Gets the platform-specific name of the dbgshim library.
    /// </summary>
    public static string ShimLibraryName => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? WindowsShimName
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? MacOSShimName
            : LinuxShimName;

    /// <summary>
    /// Attempts to resolve the path to the dbgshim library.
    /// </summary>
    /// <returns>The full path to the dbgshim library if found, null otherwise.</returns>
    public static string? TryResolve()
    {
        // Try to find dbgshim in standard locations
        foreach (var path in GetSearchPaths())
        {
            var shimPath = Path.Combine(path, ShimLibraryName);
            if (File.Exists(shimPath))
            {
                return shimPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the path to the dbgshim library.
    /// </summary>
    /// <returns>The full path to the dbgshim library.</returns>
    /// <exception cref="FileNotFoundException">Thrown when dbgshim cannot be found.</exception>
    public static string Resolve()
    {
        var path = TryResolve();
        if (path == null)
        {
            throw new FileNotFoundException(
                $"Could not locate {ShimLibraryName}. " +
                "Make sure you have the .NET SDK or runtime with debugging support installed. " +
                "On Windows, dbgshim.dll should be in the .NET installation directory. " +
                "On Linux, you may need to install the dotnet-runtime-dbg package.");
        }

        return path;
    }

    /// <summary>
    /// Gets all paths to search for the dbgshim library.
    /// </summary>
    private static IEnumerable<string> GetSearchPaths()
    {
        // 1. Try to find .NET runtime location using dotnet command
        foreach (var path in GetDotNetRuntimePaths())
        {
            yield return path;
        }

        // 2. Standard installation directories
        foreach (var standardPath in GetStandardInstallationPaths())
        {
            yield return standardPath;
        }
    }

    /// <summary>
    /// Gets runtime paths from the .NET installation.
    /// </summary>
    private static IEnumerable<string> GetDotNetRuntimePaths()
    {
        var dotnetRoot = GetDotNetRoot();
        if (dotnetRoot == null)
            yield break;

        // DbgShim should be in shared/Microsoft.NETCore.App/[version]/
        var sharedPath = Path.Combine(dotnetRoot, "shared", "Microsoft.NETCore.App");
        if (Directory.Exists(sharedPath))
        {
            var versions = Directory.GetDirectories(sharedPath)
                .OrderByDescending(v => v) // Prefer higher versions
                .ToList();

            foreach (var version in versions)
            {
                yield return version;
            }
        }

        // Also check the root dotnet directory
        yield return dotnetRoot;
    }

    /// <summary>
    /// Attempts to find the .NET installation root directory.
    /// </summary>
    private static string? GetDotNetRoot()
    {
        // Check DOTNET_ROOT environment variable first
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrEmpty(dotnetRoot) && Directory.Exists(dotnetRoot))
        {
            return dotnetRoot;
        }

        // Try to locate dotnet executable and derive root
        var dotnetExe = FindDotNetExecutable();
        if (dotnetExe != null)
        {
            // The dotnet executable is typically at the root of the .NET installation
            var dotnetDir = Path.GetDirectoryName(dotnetExe);
            if (dotnetDir != null)
            {
                // On Linux/macOS, if we found a symlink, resolve it
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var fileInfo = new FileInfo(dotnetExe);
                    if (fileInfo.LinkTarget != null)
                    {
                        var resolvedPath = Path.GetDirectoryName(Path.GetFullPath(
                            Path.Combine(dotnetDir, fileInfo.LinkTarget)));
                        if (resolvedPath != null && Directory.Exists(resolvedPath))
                        {
                            return resolvedPath;
                        }
                    }
                }

                return dotnetDir;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the dotnet executable in the system PATH.
    /// </summary>
    private static string? FindDotNetExecutable()
    {
        var execName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";
        var pathEnv = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(pathEnv))
            return null;

        var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';
        foreach (var dir in pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries))
        {
            var fullPath = Path.Combine(dir, execName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets standard installation paths for .NET on different platforms.
    /// </summary>
    private static IEnumerable<string> GetStandardInstallationPaths()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows standard paths
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            yield return Path.Combine(programFiles, "dotnet");
            yield return Path.Combine(programFilesX86, "dotnet");

            // Also check user-local installation
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            yield return Path.Combine(localAppData, "Microsoft", "dotnet");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux standard paths
            yield return "/usr/share/dotnet";
            yield return "/usr/lib/dotnet";
            yield return "/opt/dotnet";

            // User-local
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, ".dotnet");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS standard paths
            yield return "/usr/local/share/dotnet";
            yield return "/opt/homebrew/opt/dotnet";

            // User-local
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, ".dotnet");
        }
    }

    /// <summary>
    /// Gets all discovered dbgshim locations (for diagnostics).
    /// </summary>
    public static IEnumerable<(string Path, bool Exists)> GetAllSearchedPaths()
    {
        foreach (var path in GetSearchPaths())
        {
            var shimPath = Path.Combine(path, ShimLibraryName);
            yield return (shimPath, File.Exists(shimPath));
        }
    }
}
