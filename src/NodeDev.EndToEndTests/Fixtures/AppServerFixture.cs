using Microsoft.AspNetCore.Mvc.Testing;

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
}