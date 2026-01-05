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

	[Fact(Timeout = 120_000)]
	public async Task TestAutoScrollInConsoleOutput()
	{
		// Create a new project
		await HomePage.CreateNewProject();
		await Task.Delay(500);
		
		// Open the Project Explorer and select Program class
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		
		// Open the Main method to add WriteLine nodes
		await HomePage.OpenMethod("Main");
		await Task.Delay(500);
		
		// Take screenshot of initial graph
		await HomePage.TakeScreenshot("/tmp/autoscroll-initial-graph.png");
		
		// Move Return node to make space
		await HomePage.DragNodeTo("Return", 1500, 400);
		await Task.Delay(300);
		
		// Add 10 WriteLine nodes to ensure scrolling is needed
		Console.WriteLine("Adding 10 WriteLine nodes...");
		for (int i = 0; i < 10; i++)
		{
			await HomePage.AddNodeToCanvas("WriteLine");
			await Task.Delay(400);
			Console.WriteLine($"Added WriteLine node {i + 1}");
		}
		
		// Get all WriteLine nodes
		var writeLineNodes = HomePage.GetGraphNodes("WriteLine");
		Console.WriteLine($"Total WriteLine nodes: {writeLineNodes.Count}");
		
		// Set input values for each node
		for (int i = 0; i < writeLineNodes.Count && i < 10; i++)
		{
			var node = writeLineNodes[i];
			try
			{
				// Try to find the text input in the node - WriteLine has a parameter called "text"
				var inputFields = await node.Locator("input").AllAsync();
				Console.WriteLine($"Node {i} has {inputFields.Count} input fields");
				
				if (inputFields.Count > 0)
				{
					var inputField = inputFields[0];
					await inputField.FillAsync($"Line {i + 1:D3}");
					Console.WriteLine($"Set WriteLine node {i + 1} value to 'Line {i + 1:D3}'");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Warning: Could not set input for node {i}: {ex.Message}");
			}
		}
		
		// Connect all WriteLine nodes in sequence
		Console.WriteLine("Connecting nodes...");
		await HomePage.ConnectPorts("Entry", "Exec", "WriteLine", "Exec", sourceIndex: 0, targetIndex: 0);
		await Task.Delay(300);
		
		for (int i = 0; i < 9; i++)
		{
			await HomePage.ConnectPorts("WriteLine", "Exec", "WriteLine", "Exec", sourceIndex: i, targetIndex: i + 1);
			await Task.Delay(200);
		}
		
		// Connect last WriteLine to Return
		await HomePage.ConnectPorts("WriteLine", "Exec", "Return", "Exec", sourceIndex: 9, targetIndex: 0);
		await Task.Delay(300);
		
		// Take screenshot of complete graph
		await HomePage.TakeScreenshot("/tmp/autoscroll-graph-complete.png");
		
		// Run the project
		Console.WriteLine("Running project...");
		var runButton = Page.Locator("[data-test-id='run-project']");
		await runButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
		await runButton.ClickAsync();
		await Task.Delay(3000); // Wait for execution to complete
		
		// Verify console panel is visible
		var isVisible = await HomePage.IsConsolePanelVisible();
		Assert.True(isVisible, "Console panel should be visible after running project");
		
		// Make sure Console Output tab is active
		var consoleOutputTab = Page.Locator(".consoleTabs").GetByText("Console Output");
		await consoleOutputTab.ClickAsync();
		await Task.Delay(500);
		
		// Take screenshot of console output - should show bottom lines due to auto-scroll
		await HomePage.TakeScreenshot("/tmp/autoscroll-console-with-content.png");
		
		// Get all console lines to verify content
		var consoleLines = Page.Locator(".consoleLine");
		var lineCount = await consoleLines.CountAsync();
		Console.WriteLine($"Console has {lineCount} lines");
		
		// If we have lines, check the content
		if (lineCount > 0)
		{
			var firstLineText = await consoleLines.First.TextContentAsync();
			var lastLineText = await consoleLines.Last.TextContentAsync();
			Console.WriteLine($"First line: {firstLineText}");
			Console.WriteLine($"Last line: {lastLineText}");
		}
		
		// Verify scroll position - should be at 0 (top of flex-column-reverse = bottom visually)
		var scrollPosition = await HomePage.GetConsoleScrollPosition();
		Console.WriteLine($"Scroll position with auto-scroll enabled: {scrollPosition}");
		Assert.True(scrollPosition <= 10, $"Auto-scroll should scroll to top (scrollTop near 0), but got {scrollPosition}");
		
		// Verify auto-scroll toggle button exists and is visible
		var toggleButton = Page.Locator("[data-test-id='autoScrollToggle']");
		await toggleButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
		Console.WriteLine("✓ Auto-scroll toggle button is visible");
		
		// Disable auto-scroll
		await HomePage.ToggleAutoScroll();
		await Task.Delay(200);
		Console.WriteLine("✓ Disabled auto-scroll");
		
		// Scroll to bottom (which is actually scrollHeight for flex-column-reverse)
		var container = Page.Locator("[data-test-id='consoleOutputContainer']");
		await container.EvaluateAsync("el => el.scrollTop = el.scrollHeight");
		await Task.Delay(500);
		
		// Take screenshot with auto-scroll disabled and scrolled away
		await HomePage.TakeScreenshot("/tmp/autoscroll-disabled-scrolled.png");
		
		var scrolledPosition = await HomePage.GetConsoleScrollPosition();
		Console.WriteLine($"Scroll position after manual scroll: {scrolledPosition}");
		
		// Re-enable auto-scroll
		await HomePage.ToggleAutoScroll();
		await Task.Delay(200);
		Console.WriteLine("✓ Re-enabled auto-scroll");
		
		// Run again to trigger auto-scroll with new content
		await runButton.ClickAsync();
		await Task.Delay(3000);
		
		// Take final screenshot - should auto-scroll to show latest output
		await HomePage.TakeScreenshot("/tmp/autoscroll-final-with-new-content.png");
		
		// Verify it scrolled back to show newest items
		var finalScrollPosition = await HomePage.GetConsoleScrollPosition();
		Console.WriteLine($"Scroll position after re-run with auto-scroll: {finalScrollPosition}");
		Assert.True(finalScrollPosition <= 10, $"Auto-scroll should work after re-enabling, but got {finalScrollPosition}");
		
		Console.WriteLine("✓ Auto-scroll test completed successfully with 10 WriteLine nodes");
	}
}
