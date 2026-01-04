using System.Runtime.InteropServices;
using Xunit;

namespace NodeDev.EndToEndTests;

/// <summary>
/// Custom Fact attribute that skips tests on Linux when running in CI environments.
/// This is useful for tests that use features incompatible with Linux CI runners,
/// such as ICorDebug/ptrace-based debugging which can conflict with the test host process.
/// </summary>
public class SkipOnLinuxCIFactAttribute : FactAttribute
{
	public SkipOnLinuxCIFactAttribute(string reason = "Test uses features incompatible with Linux CI")
	{
		if (ShouldSkip())
		{
			Skip = reason;
		}
	}

	private static bool ShouldSkip()
	{
		// Skip if we're on Linux and in a CI environment
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return false;
		}

		// Check for common CI environment variables
		var ciIndicators = new[]
		{
			"CI",
			"GITHUB_ACTIONS",
			"GITLAB_CI",
			"CIRCLECI",
			"TRAVIS",
			"JENKINS_URL",
			"TEAMCITY_VERSION"
		};

		return ciIndicators.Any(indicator =>
		{
			var value = Environment.GetEnvironmentVariable(indicator);
			return !string.IsNullOrEmpty(value) && !value.Equals("false", StringComparison.OrdinalIgnoreCase);
		});
	}
}
