using Microsoft.Playwright;
using NodeDev.EndToEndTests.Fixtures;
using NodeDev.EndToEndTests.Pages;

namespace NodeDev.EndToEndTests;

[Collection("E2E")]
public abstract class E2ETestBase : IAsyncLifetime
{
	protected readonly AppServerFixture App;
	protected readonly PlaywrightFixture Playwright;
	protected IPage Page { get; private set; } = null!;
	protected HomePage HomePage { get; private set; } = null!;
	protected readonly List<string> ConsoleErrors = new();
	protected readonly List<string> ConsoleWarnings = new();

	protected E2ETestBase(AppServerFixture app, PlaywrightFixture playwright)
	{
		App = app;
		Playwright = playwright;
	}

	public virtual async Task InitializeAsync()
	{
		if (Playwright.Browser == null)
			throw new InvalidOperationException("Browser not initialized");

		var context = await Playwright.Browser.NewContextAsync(new()
		{
			ViewportSize = new()
			{
				Width = 1900,
				Height = 1000
			}
		});

		// Set default timeout to 5 seconds for faster test failures
		context.SetDefaultTimeout(5000);

		Page = await context.NewPageAsync();
		HomePage = new HomePage(Page);

		// Navigate to the app
		await Page.GotoAsync(App.BaseUrl.ToString());
	}

	public virtual async Task DisposeAsync()
	{
		if (Page != null)
		{
			await Page.Context.CloseAsync();
		}
	}

	protected void SetupConsoleMonitoring()
	{
		ConsoleErrors.Clear();
		ConsoleWarnings.Clear();

		Page.Console += (_, msg) =>
		{
			var msgType = msg.Type;
			var text = msg.Text;

			Console.WriteLine($"[BROWSER {msgType.ToUpper()}] {text}");

			if (msgType == "error")
			{
				ConsoleErrors.Add(text);
			}
			else if (msgType == "warning")
			{
				ConsoleWarnings.Add(text);
			}
		};

		Page.PageError += (_, error) =>
		{
			Console.WriteLine($"[PAGE ERROR] {error}");
			ConsoleErrors.Add(error);
		};
	}

	protected void AssertNoConsoleErrors()
	{
		if (ConsoleErrors.Count > 0)
		{
			var errorList = string.Join("\n  - ", ConsoleErrors);
			throw new Exception($"Found {ConsoleErrors.Count} console error(s):\n  - {errorList}");
		}

		Console.WriteLine("✓ No console errors detected");
	}
}
