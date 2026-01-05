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

	[Fact(Timeout = 90_000)]
	public async Task TestAutoScrollInConsoleOutput()
	{
		// Create a new project
		await HomePage.CreateNewProject();
		
		// Open the Main method in the Program class
		await HomePage.OpenMethod("Main");
		await Task.Delay(500);
		
		// Take screenshot of initial state
		await HomePage.TakeScreenshot("/tmp/autoscroll-initial.png");
		
		// Add multiple WriteLine nodes to generate enough output to require scrolling
		for (int i = 0; i < 10; i++)
		{
			await HomePage.AddNodeToCanvas("WriteLine");
			await Task.Delay(200);
			
			// Set the input value for each WriteLine node
			var writeLineNodes = HomePage.GetGraphNodes("WriteLine");
			var count = writeLineNodes.Count;
			if (count > i)
			{
				var node = writeLineNodes[i];
				var inputField = node.Locator(".col.input").Filter(new() { HasText = "text" }).Locator("input").First;
				await inputField.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
				await inputField.FillAsync($"Line {i + 1}");
			}
		}
		
		// Connect all WriteLine nodes in sequence
		// First connect Entry to first WriteLine
		await HomePage.ConnectPorts("Entry", "Exec", "WriteLine", "Exec", sourceIndex: 0, targetIndex: 0);
		await Task.Delay(300);
		
		// Connect WriteLine nodes in sequence
		for (int i = 0; i < 9; i++)
		{
			await HomePage.ConnectPorts("WriteLine", "Exec", "WriteLine", "Exec", sourceIndex: i, targetIndex: i + 1);
			await Task.Delay(300);
		}
		
		// Connect last WriteLine to Return
		await HomePage.ConnectPorts("WriteLine", "Exec", "Return", "Exec", sourceIndex: 9, targetIndex: 0);
		await Task.Delay(300);
		
		// Take screenshot of graph with all nodes
		await HomePage.TakeScreenshot("/tmp/autoscroll-graph-setup.png");
		
		// Run the project
		await HomePage.RunProject();
		await Task.Delay(2000); // Wait for execution
		
		// Verify console panel is visible
		var isVisible = await HomePage.IsConsolePanelVisible();
		Assert.True(isVisible, "Console panel should be visible after running project");
		
		// Take screenshot of console output
		await HomePage.TakeScreenshot("/tmp/autoscroll-console-output.png");
		
		// Check that auto-scroll is enabled by default
		// For flex-column-reverse, scrollTop should be 0 when showing newest items
		var scrollPosition = await HomePage.GetConsoleScrollPosition();
		Console.WriteLine($"Scroll position with auto-scroll: {scrollPosition}");
		
		// scrollTop should be 0 for newest items in flex-column-reverse
		Assert.True(scrollPosition <= 10, $"Auto-scroll should scroll to top (scrollTop near 0), but got {scrollPosition}");
		
		// Now disable auto-scroll
		await HomePage.ToggleAutoScroll();
		await Task.Delay(200);
		
		// Scroll manually to simulate user looking at old logs
		var container = Page.Locator("[data-test-id='consoleOutputContainer']");
		await container.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
		await Task.Delay(200);
		
		var scrolledPosition = await HomePage.GetConsoleScrollPosition();
		Console.WriteLine($"Scroll position after manual scroll: {scrolledPosition}");
		
		// Take screenshot showing auto-scroll disabled
		await HomePage.TakeScreenshot("/tmp/autoscroll-disabled.png");
		
		// Re-enable auto-scroll
		await HomePage.ToggleAutoScroll();
		await Task.Delay(200);
		
		// Run again to generate more output
		await HomePage.RunProject();
		await Task.Delay(2000);
		
		// Verify it scrolled back to show newest items
		var finalScrollPosition = await HomePage.GetConsoleScrollPosition();
		Console.WriteLine($"Scroll position after re-enabling auto-scroll: {finalScrollPosition}");
		Assert.True(finalScrollPosition <= 10, $"Auto-scroll should work after re-enabling, but got {finalScrollPosition}");
		
		// Take final screenshot
		await HomePage.TakeScreenshot("/tmp/autoscroll-final.png");
		
		Console.WriteLine("✓ Auto-scroll test completed successfully");
	}
}
