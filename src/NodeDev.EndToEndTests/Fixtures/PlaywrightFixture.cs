using Microsoft.Playwright;
using Xunit;

namespace NodeDev.EndToEndTests.Fixtures;

public class PlaywrightFixture : IAsyncLifetime
{
	public IPlaywright? Playwright { get; private set; }
	public IBrowser? Browser { get; private set; }

	public async Task InitializeAsync()
	{
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
