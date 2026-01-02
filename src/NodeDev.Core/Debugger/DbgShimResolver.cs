using System.Runtime.InteropServices;

namespace NodeDev.Core.Debugger;

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
	/// Gets the runtime identifier (RID) for the current platform.
	/// </summary>
	private static string RuntimeIdentifier
	{
		get
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return RuntimeInformation.OSArchitecture switch
				{
					Architecture.X64 => "win-x64",
					Architecture.X86 => "win-x86",
					Architecture.Arm64 => "win-arm64",
					_ => "win-x64"
				};
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				return RuntimeInformation.OSArchitecture switch
				{
					Architecture.X64 => "linux-x64",
					Architecture.Arm64 => "linux-arm64",
					Architecture.Arm => "linux-arm",
					_ => "linux-x64"
				};
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				return RuntimeInformation.OSArchitecture switch
				{
					Architecture.X64 => "osx-x64",
					Architecture.Arm64 => "osx-arm64",
					_ => "osx-x64"
				};
			}
			return "linux-x64";
		}
	}

	/// <summary>
	/// Attempts to resolve the path to the dbgshim library.
	/// </summary>
	/// <returns>The full path to the dbgshim library if found, null otherwise.</returns>
	public static string? TryResolve()
	{
		// Try to find dbgshim in standard locations
		foreach (var path in GetSearchPaths())
		{
			if (File.Exists(path))
			{
				return path;
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
				"Make sure you have the Microsoft.Diagnostics.DbgShim NuGet package installed. " +
				"On Windows, dbgshim.dll should be in the .NET installation directory. " +
				"On Linux, the package provides libdbgshim.so in the NuGet cache.");
		}

		return path;
	}

	/// <summary>
	/// Gets all paths to search for the dbgshim library.
	/// </summary>
	private static IEnumerable<string> GetSearchPaths()
	{
		// 1. Check NuGet packages first (Microsoft.Diagnostics.DbgShim)
		foreach (var path in GetNuGetPackagePaths())
		{
			yield return path;
		}

		// 2. Try to find .NET runtime location using dotnet command
		foreach (var path in GetDotNetRuntimePaths())
		{
			yield return path;
		}

		// 3. Standard installation directories
		foreach (var standardPath in GetStandardInstallationPaths())
		{
			yield return standardPath;
		}
	}

	/// <summary>
	/// Gets paths to search in NuGet package cache for Microsoft.Diagnostics.DbgShim.
	/// </summary>
	private static IEnumerable<string> GetNuGetPackagePaths()
	{
		var nugetCache = GetNuGetPackagesCachePath();
		if (nugetCache == null)
			yield break;

		// The package is microsoft.diagnostics.dbgshim.[rid]
		// e.g., microsoft.diagnostics.dbgshim.linux-x64
		var rid = RuntimeIdentifier;
		var packagePattern = $"microsoft.diagnostics.dbgshim.{rid}";
		var packageDir = Path.Combine(nugetCache, packagePattern);

		if (Directory.Exists(packageDir))
		{
			// Find the latest version
			var versions = Directory.GetDirectories(packageDir)
				.OrderByDescending(v => v)
				.ToList();

			foreach (var version in versions)
			{
				// The native library is in runtimes/[rid]/native/
				var nativePath = Path.Combine(version, "runtimes", rid, "native", ShimLibraryName);
				yield return nativePath;
			}
		}

		// Also check the base package without RID for meta-package references
		var basePackageDir = Path.Combine(nugetCache, "microsoft.diagnostics.dbgshim");
		if (Directory.Exists(basePackageDir))
		{
			var versions = Directory.GetDirectories(basePackageDir)
				.OrderByDescending(v => v)
				.ToList();

			foreach (var version in versions)
			{
				var nativePath = Path.Combine(version, "runtimes", rid, "native", ShimLibraryName);
				yield return nativePath;
			}
		}
	}

	/// <summary>
	/// Gets the NuGet packages cache path.
	/// </summary>
	private static string? GetNuGetPackagesCachePath()
	{
		// Check NUGET_PACKAGES environment variable first
		var nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
		if (!string.IsNullOrEmpty(nugetPackages) && Directory.Exists(nugetPackages))
		{
			return nugetPackages;
		}

		// Default NuGet packages location
		var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var defaultPath = Path.Combine(home, ".nuget", "packages");
		if (Directory.Exists(defaultPath))
		{
			return defaultPath;
		}

		return null;
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
				yield return Path.Combine(version, ShimLibraryName);
			}
		}

		// Also check the root dotnet directory
		yield return Path.Combine(dotnetRoot, ShimLibraryName);
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
					try
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
					catch
					{
						// LinkTarget not supported or symlink resolution failed - fall through to default
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

			yield return Path.Combine(programFiles, "dotnet", ShimLibraryName);
			yield return Path.Combine(programFilesX86, "dotnet", ShimLibraryName);

			// Also check user-local installation
			var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			yield return Path.Combine(localAppData, "Microsoft", "dotnet", ShimLibraryName);
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			// Linux standard paths
			yield return Path.Combine("/usr/share/dotnet", ShimLibraryName);
			yield return Path.Combine("/usr/lib/dotnet", ShimLibraryName);
			yield return Path.Combine("/opt/dotnet", ShimLibraryName);

			// User-local
			var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			yield return Path.Combine(home, ".dotnet", ShimLibraryName);
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			// macOS standard paths
			yield return Path.Combine("/usr/local/share/dotnet", ShimLibraryName);
			yield return Path.Combine("/opt/homebrew/opt/dotnet", ShimLibraryName);

			// User-local
			var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			yield return Path.Combine(home, ".dotnet", ShimLibraryName);
		}
	}

	/// <summary>
	/// Gets all discovered dbgshim locations (for diagnostics).
	/// </summary>
	public static IEnumerable<(string Path, bool Exists)> GetAllSearchedPaths()
	{
		foreach (var path in GetSearchPaths())
		{
			yield return (path, File.Exists(path));
		}
	}
}
