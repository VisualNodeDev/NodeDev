using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NodeDev.EndToEndTests.Fixtures;

public class AppServerFixture : IAsyncLifetime
{
	private BlazorWebAppFactory? _factory;

	public Uri BaseUrl => _factory?.ClientOptions.BaseAddress ?? throw new InvalidOperationException("BlazorWebAppFactory not initialized");

	public async Task InitializeAsync()
	{
		_factory = new BlazorWebAppFactory();

		_factory.UseKestrel();
		_factory.StartServer();
	}

	public async Task DisposeAsync()
	{
		if (_factory != null)
			await _factory.DisposeAsync();
	}
}

internal class BlazorWebAppFactory : WebApplicationFactory<Program>
{
	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		builder.UseEnvironment("Development");
		builder.ConfigureLogging(logging =>
		{
			logging.ClearProviders();
			logging.AddConsole();
		});
		builder.ConfigureServices(services =>
			services.AddDataProtection().UseEphemeralDataProtectionProvider());
	}
}
