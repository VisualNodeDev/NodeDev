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
	public async Task TestConsolePanelButtonsAlwaysVisible()
	{
		await HomePage.CreateNewProject();
		
		// Console panel should always be visible (even when collapsed)
		var consolePanel = Page.Locator("[data-test-id='consolePanel']");
		await consolePanel.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 5000 });
		
		// Initially collapsed, so expand button should be visible
		var expandButton = Page.Locator("[data-test-id='showConsoleButton']");
		var isExpandButtonVisible = await expandButton.IsVisibleAsync();
		Console.WriteLine($"Initial state - Expand button visible: {isExpandButtonVisible}");
		
		// Take screenshot of initial state
		await HomePage.TakeScreenshot("/tmp/console-initial-collapsed.png");
		
		// Click to expand
		await expandButton.ClickAsync();
		await Task.Delay(500); // Wait for animation
		
		// Collapse button should be visible now
		var collapseButton = Page.Locator("[data-test-id='collapseConsoleButton']");
		var isCollapseButtonVisible = await collapseButton.IsVisibleAsync();
		Console.WriteLine($"After expand - Collapse button visible: {isCollapseButtonVisible}");
		
		// Tabs should be visible when expanded
		var tabs = Page.Locator(".consoleTabs");
		var isTabsVisible = await tabs.IsVisibleAsync();
		Console.WriteLine($"After expand - Tabs visible: {isTabsVisible}");
		
		// Take screenshot of expanded state
		await HomePage.TakeScreenshot("/tmp/console-expanded.png");
		
		Assert.True(isCollapseButtonVisible, "Collapse button should be visible after expanding");
		Assert.True(isTabsVisible, "Tabs should be visible after expanding");
		
		// Click to collapse again
		await collapseButton.ClickAsync();
		await Task.Delay(500); // Wait for animation
		
		// Expand button should be visible again
		isExpandButtonVisible = await expandButton.IsVisibleAsync();
		Console.WriteLine($"After collapse - Expand button visible: {isExpandButtonVisible}");
		
		// Take screenshot of collapsed state again
		await HomePage.TakeScreenshot("/tmp/console-collapsed-again.png");
		
		Assert.True(isExpandButtonVisible, "Expand button should be visible after collapsing");
	}
	
	[Fact(Timeout = 60_000)]
	public async Task TestConsolePanelPersistsAfterRunning()
	{
		await HomePage.CreateNewProject();
		
		// Expand the console panel first
		var consolePanel = Page.Locator("[data-test-id='consolePanel']");
		await consolePanel.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 5000 });
		
		var expandButton = Page.Locator("[data-test-id='showConsoleButton']");
		if (await expandButton.IsVisibleAsync())
		{
			await expandButton.ClickAsync();
			await Task.Delay(500);
		}
		
		// Take screenshot before running
		await HomePage.TakeScreenshot("/tmp/console-before-run.png");
		
		// The project starts with a default Main method
		// Try to run it (even though it might have no output)
		var runButton = Page.Locator("[data-test-id='run-project']");
		if (await runButton.CountAsync() > 0)
		{
			await runButton.ClickAsync();
			await Task.Delay(2000); // Wait for execution
			
			// Take screenshot after running
			await HomePage.TakeScreenshot("/tmp/console-after-run.png");
			
			// Verify collapse button is still there
			var collapseButton = Page.Locator("[data-test-id='collapseConsoleButton']");
			var isCollapseButtonVisible = await collapseButton.IsVisibleAsync();
			Console.WriteLine($"After running - Collapse button visible: {isCollapseButtonVisible}");
			
			// Verify tabs are still there
			var tabs = Page.Locator(".consoleTabs");
			var isTabsVisible = await tabs.IsVisibleAsync();
			Console.WriteLine($"After running - Tabs visible: {isTabsVisible}");
			
			Assert.True(isCollapseButtonVisible, "Collapse button should remain visible after running");
			Assert.True(isTabsVisible, "Tabs should remain visible after running");
		}
		else
		{
			Console.WriteLine("Run button not found - skipping run test");
		}
	}
}
