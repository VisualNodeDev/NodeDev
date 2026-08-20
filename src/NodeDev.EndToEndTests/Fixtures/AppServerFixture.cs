using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NodeDev.Blazor.Services;

namespace NodeDev.EndToEndTests.Fixtures;

public class AppServerFixture : IAsyncLifetime
{
	private BlazorWebAppFactory? _factory;
	private string? TestDataDirectory;

	public Uri BaseUrl => _factory?.ClientOptions.BaseAddress ?? throw new InvalidOperationException("BlazorWebAppFactory not initialized");

	public async Task InitializeAsync()
	{
		TestDataDirectory = Path.Combine(Path.GetTempPath(), "NodeDev.EndToEndTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(TestDataDirectory);
		_factory = new BlazorWebAppFactory(TestDataDirectory);

		_factory.UseKestrel();
		_factory.StartServer();
	}

	public async Task DisposeAsync()
	{
		if (_factory != null)
			await _factory.DisposeAsync();

		if (TestDataDirectory != null && Directory.Exists(TestDataDirectory))
			Directory.Delete(TestDataDirectory, recursive: true);
	}
}

internal class BlazorWebAppFactory(string testDataDirectory) : WebApplicationFactory<Program>
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
		{
			services.AddDataProtection().UseEphemeralDataProtectionProvider();

			services.RemoveAll<AppOptionsContainer>();
			var optionsContainer = new AppOptionsContainer(Path.Combine(testDataDirectory, "AppOptions.json"));
			optionsContainer.AppOptions = new AppOptions
			{
				ProjectsDirectory = Path.Combine(testDataDirectory, "Projects")
			};
			services.AddSingleton(optionsContainer);
		});
	}
}
