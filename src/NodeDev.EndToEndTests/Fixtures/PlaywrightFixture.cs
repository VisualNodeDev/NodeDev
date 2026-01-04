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
		
		// Always use headless mode on CI or when no display is available (Linux)
		var isHeadless = true;
#if DEBUG
		// Only use headed mode if we have a display available
		if (Environment.GetEnvironmentVariable("DISPLAY") != null || OperatingSystem.IsWindows())
		{
			isHeadless = false;
		}
#endif
		
		Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
		{
			Headless = isHeadless
		});
	}

	public async Task DisposeAsync()
	{
		if (Browser != null)
			await Browser.DisposeAsync();

		Playwright?.Dispose();
	}
}
