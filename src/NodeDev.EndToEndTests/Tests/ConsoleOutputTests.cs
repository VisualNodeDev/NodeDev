using NodeDev.EndToEndTests.Fixtures;
using Xunit;

namespace NodeDev.EndToEndTests.Tests;

public class ConsoleOutputTests : E2ETestBase
{
	public ConsoleOutputTests(AppServerFixture app, PlaywrightFixture playwright)
		: base(app, playwright)
	{
	}

	[Fact(Timeout = 60_000)]
	public async Task TestConsoleOutputAppears()
	{
		await HomePage.CreateNewProject();
		
		// This is a placeholder test since the feature file was empty
		// In a real scenario, this would test that console output from WriteLine nodes
		// appears in the bottom panel when running the project
		
		var isConsolePanelVisible = await HomePage.IsConsolePanelVisible();
		Console.WriteLine($"Console panel visible: {isConsolePanelVisible}");
		
		await HomePage.TakeScreenshot("/tmp/console-output-test.png");
	}
}
