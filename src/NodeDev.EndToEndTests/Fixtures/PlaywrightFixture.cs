using Microsoft.Playwright;
using Xunit;

namespace NodeDev.EndToEndTests.Fixtures;

public class PlaywrightFixture : IAsyncLifetime
{
	public IPlaywright? Playwright { get; private set; }
	public IBrowser? Browser { get; private set; }

	public async Task InitializeAsync()
	{
		// Install Playwright if needed
		var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
		if (exitCode != 0)
		{
			Console.WriteLine($"Warning: Playwright install returned code {exitCode}");
		}

		Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
		Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
		{
			Headless = Environment.GetEnvironmentVariable("HEADLESS") != "false"
		});
	}

	public async Task DisposeAsync()
	{
		if (Browser != null)
			await Browser.DisposeAsync();
		
		Playwright?.Dispose();
	}
}
