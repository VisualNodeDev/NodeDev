using System.Diagnostics;
using Xunit;

namespace NodeDev.EndToEndTests.Fixtures;

public class AppServerFixture : IAsyncLifetime
{
	private Process? _serverProcess;
	private const int Port = 5166;

	public Uri BaseUrl => new Uri($"http://localhost:{Port}");

	public async Task InitializeAsync()
	{
		_serverProcess = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = $"run --no-build --project NodeDev.Blazor.Server.csproj -- --urls http://localhost:{Port}",
				WorkingDirectory = "../../../../NodeDev.Blazor.Server",
				UseShellExecute = false,
				RedirectStandardOutput = false,
				RedirectStandardError = false
			}
		};

		_serverProcess.Start();

		// Wait for server to start
		await Task.Delay(3000);

		// Verify it's running
		if (_serverProcess.HasExited)
		{
			throw new Exception($"Failed to start the server: exit code {_serverProcess.ExitCode}");
		}
	}

	public async Task DisposeAsync()
	{
		if (_serverProcess != null && !_serverProcess.HasExited)
		{
			_serverProcess.Kill(true);
			while (!_serverProcess.HasExited)
			{
				await Task.Delay(100);
			}
		}
	}
}
