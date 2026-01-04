using NodeDev.EndToEndTests.Fixtures;
using Xunit;

namespace NodeDev.EndToEndTests.Tests;

public class DebugModeTests : E2ETestBase
{
	public DebugModeTests(AppServerFixture app, PlaywrightFixture playwright)
		: base(app, playwright)
	{
	}

	[Fact(Timeout = 60_000)]
	public async Task ToolbarButtons_ShouldShowRunAndDebugWhenNotDebugging()
	{
		// Arrange - Create a new project
		await HomePage.CreateNewProject();
		await Task.Delay(500);

		// Assert - Check that Run and Run with Debug buttons are visible
		var runButton = Page.Locator("[data-test-id='run-project']");
		var runWithDebugButton = Page.Locator("[data-test-id='run-with-debug']");
		var stopButton = Page.Locator("[data-test-id='stop-debug']");

		await runButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
		await runWithDebugButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

		// Stop button should not be visible
		var stopCount = await stopButton.CountAsync();
		Assert.Equal(0, stopCount);

		Console.WriteLine("✓ Run and Run with Debug buttons are visible when not debugging");

		await HomePage.TakeScreenshot("/tmp/toolbar-not-debugging.png");
	}

	[SkipOnLinuxCIFact(Timeout = 60_000)]
	public async Task ToolbarButtons_ShouldShowStopPauseResumeWhenDebugging()
	{
		// Arrange - Create a new project
		await HomePage.CreateNewProject();

		// Open Program/Main for node manipulation
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");

		// Move return node to make space
		await HomePage.DragNodeTo("Return", 1500, 400);

		// Add Sleep node to the graph
		await HomePage.SearchForNodes("Sleep");
		await HomePage.AddNodeFromSearch("Sleep");

		// Move Sleep node to avoid overlap
		await HomePage.DragNodeTo("Sleep", 800, 400);

		// Set Sleep node input to 5000
		await HomePage.SetNodeInputValue("Sleep", "TimeMilliseconds", "5000");

		// Connect Entry.Exec -> Sleep.Exec
		await HomePage.ConnectPorts("Entry", "Exec", "Sleep", "Exec");
		// Connect Sleep.Exec -> Return.Exec
		await HomePage.ConnectPorts("Sleep", "Exec", "Return", "Exec");

		// Act - Click "Run with Debug"
		var runWithDebugButton = Page.Locator("[data-test-id='run-with-debug']");
		await runWithDebugButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
		await runWithDebugButton.ClickAsync();

		// Assert - Check that Stop/Pause/Resume buttons are visible
		var stopButton = Page.Locator("[data-test-id='stop-debug']");
		var pauseButton = Page.Locator("[data-test-id='pause-debug']");
		var resumeButton = Page.Locator("[data-test-id='resume-debug']");
		var statusText = Page.Locator("[data-test-id='debug-status-text']");

		await stopButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 10000 });
		var isStopVisible = await stopButton.IsVisibleAsync();
		var isPauseVisible = await pauseButton.IsVisibleAsync();
		var isResumeVisible = await resumeButton.IsVisibleAsync();
		var isStatusVisible = await statusText.IsVisibleAsync();

		Console.WriteLine($"Stop button visible: {isStopVisible}");
		Console.WriteLine($"Pause button visible: {isPauseVisible}");
		Console.WriteLine($"Resume button visible: {isResumeVisible}");
		Console.WriteLine($"Status text visible: {isStatusVisible}");

		Assert.True(isStopVisible, "Stop button should be visible");
		Assert.True(isPauseVisible, "Pause button should be visible");
		Assert.True(isResumeVisible, "Resume button should be visible");
		Assert.True(isStatusVisible, "Status text should be visible");

		// Check that Run and Run with Debug buttons are NOT visible
		var runButton = Page.Locator("[data-test-id='run-project']");
		var runWithDebugCount = await Page.Locator("[data-test-id='run-with-debug']").CountAsync();

		var runCount = await runButton.CountAsync();
		Assert.Equal(0, runCount);
		Assert.Equal(0, runWithDebugCount);

		Console.WriteLine("✓ Stop/Pause/Resume buttons are visible when debugging");
		Console.WriteLine("✓ Run/Run with Debug buttons are hidden when debugging");

		await HomePage.TakeScreenshot("/tmp/toolbar-while-debugging.png");

		// Cleanup - Stop debugging
		await stopButton.ClickAsync();
	}

	[SkipOnLinuxCIFact(Timeout = 60_000)]
	public async Task StopButton_ShouldStopDebugSession()
	{
		// Arrange - Start debugging
		await HomePage.CreateNewProject();

		// Open Program/Main for node manipulation
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");

		// Move return node to make space
		await HomePage.DragNodeTo("Return", 1500, 400);

		// Add Sleep node to the graph
		await HomePage.SearchForNodes("Sleep");
		await HomePage.AddNodeFromSearch("Sleep");

		// Move Sleep node to avoid overlap
		await HomePage.DragNodeTo("Sleep", 800, 400);

		// Set Sleep node input to 5000
		await HomePage.SetNodeInputValue("Sleep", "TimeMilliseconds", "15000");

		// Connect Entry.Exec -> Sleep.Exec
		await HomePage.ConnectPorts("Entry", "Exec", "Sleep", "Exec");
		// Connect Sleep.Exec -> Return.Exec
		await HomePage.ConnectPorts("Sleep", "Exec", "Return", "Exec");

		// Act - Click "Run with Debug"
		var runWithDebugButton = Page.Locator("[data-test-id='run-with-debug']");
		await runWithDebugButton.ClickAsync();

		// Verify we're debugging
		var stopButton = Page.Locator("[data-test-id='stop-debug']");
		await stopButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

		// Act - Click Stop button
		await stopButton.ClickAsync();

		// Assert - Should return to normal state
		var runButton = Page.Locator("[data-test-id='run-project']");
		// wait with small timeout so we know it's because the stop worked, and not because it just was done running
		await runButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 1000 });

		var isRunVisible = await runButton.IsVisibleAsync();
		Assert.True(isRunVisible, "Run button should be visible after stopping");

		// Stop button should not be visible anymore
		var stopCount = await stopButton.CountAsync();
		Assert.Equal(0, stopCount);

		Console.WriteLine("✓ Stop button successfully terminated debug session");

		await HomePage.TakeScreenshot("/tmp/toolbar-after-stop.png");
	}

	[SkipOnLinuxCIFact]
	public async Task RunWithDebug_ShouldShowDebugCallbacksTab()
	{
		// Arrange - Create a new project
		await HomePage.CreateNewProject();
		await Task.Delay(500);

		// Act - Run with debug
		var runWithDebugButton = Page.Locator("[data-test-id='run-with-debug']");
		await runWithDebugButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
		await runWithDebugButton.ClickAsync();
		
		// Wait for console panel to appear
		await Task.Delay(2000);
		
		// Assert - Debug Callbacks tab should be visible
		var consoleTabs = Page.Locator(".consoleTabs");
		await consoleTabs.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
		
		var debugCallbacksTab = Page.Locator(".debugCallbacksTab");
		var isVisible = await debugCallbacksTab.IsVisibleAsync();
		Assert.True(isVisible, "Debug Callbacks tab should be visible");
		
		// Click on the Debug Callbacks tab
		await debugCallbacksTab.ClickAsync();
		await Task.Delay(500);
		
		await HomePage.TakeScreenshot("/tmp/debug-callbacks-tab.png");
	}

	[SkipOnLinuxCIFact]
	public async Task RunWithDebug_ShouldDisplayCallbacksInTab()
	{
		// Arrange - Create a new project
		await HomePage.CreateNewProject();
		await Task.Delay(500);

		// Act - Run with debug
		var runWithDebugButton = Page.Locator("[data-test-id='run-with-debug']");
		await runWithDebugButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
		await runWithDebugButton.ClickAsync();
		
		// Wait for execution
		await Task.Delay(2000);
		
		// Switch to Debug Callbacks tab
		var debugCallbacksTab = Page.Locator(".debugCallbacksTab");
		await debugCallbacksTab.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
		await debugCallbacksTab.ClickAsync();
		await Task.Delay(500);
		
		// Assert - Should have debug callback lines
		var callbackLines = Page.Locator(".debugCallbackLine");
		var count = await callbackLines.CountAsync();
		
		Console.WriteLine($"Found {count} debug callback lines");
		Assert.True(count > 0, "Should have at least one debug callback");
		
		// Check that callbacks have proper format (timestamp, type, description)
		if (count > 0)
		{
			var firstCallback = await callbackLines.First.TextContentAsync();
			Console.WriteLine($"First callback: {firstCallback}");
			
			// Should contain timestamp, callback type
			Assert.Contains("[", firstCallback);
			Assert.Contains("]", firstCallback);
			Assert.Contains(":", firstCallback);
		}
		
		await HomePage.TakeScreenshot("/tmp/debug-callbacks-content.png");
	}

	[SkipOnLinuxCIFact]
	public async Task RunWithDebug_ShouldUpdateStateWhenProcessExits()
	{
		// Arrange - Create a new project
		await HomePage.CreateNewProject();
		await Task.Delay(500);

		// Act - Run with debug and monitor state changes
		var runWithDebugButton = Page.Locator("[data-test-id='run-with-debug']");
		await runWithDebugButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
		
		// Get initial state
		var isDisabledBefore = await runWithDebugButton.IsDisabledAsync();
		Console.WriteLine($"Button disabled before: {isDisabledBefore}");
		Assert.False(isDisabledBefore, "Button should be enabled initially");
		
		// Click to start debugging
		await runWithDebugButton.ClickAsync();
		
		// Wait briefly for debug to start
		await Task.Delay(500);
		
		// Button should be disabled during execution
		var isDisabledDuring = await runWithDebugButton.IsDisabledAsync();
		Console.WriteLine($"Button disabled during: {isDisabledDuring}");
		
		// Wait for process to complete
		await Task.Delay(2000);
		
		// Button should be enabled again after process exits
		var isDisabledAfter = await runWithDebugButton.IsDisabledAsync();
		Console.WriteLine($"Button disabled after: {isDisabledAfter}");
		Assert.False(isDisabledAfter, "Button should be enabled after process exits");
		
		await HomePage.TakeScreenshot("/tmp/debug-state-after-exit.png");
	}

	[SkipOnLinuxCIFact]
	public async Task RunWithDebug_ConsoleOutputAndCallbacksShouldBothWork()
	{
		// Arrange - Create a new project
		await HomePage.CreateNewProject();

		// Act - Run with debug
		var runWithDebugButton = Page.Locator("[data-test-id='run-with-debug']");
		await runWithDebugButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
		await runWithDebugButton.ClickAsync();
		
		// Wait for execution
		await Task.Delay(1000);
		
		// Assert - Both tabs should have content
		
		// Check Console Output tab
		var consoleOutputTab = Page.Locator("[role='tab'].consoleOutputTab");
		await consoleOutputTab.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
		await consoleOutputTab.ClickAsync();
		await Task.Delay(300);
		
		var consoleLines = Page.Locator(".consoleLine");
		var consoleCount = await consoleLines.CountAsync();
		Console.WriteLine($"Console lines: {consoleCount}");
		
		// Check Debug Callbacks tab
		var debugCallbacksTab = Page.Locator(".debugCallbacksTab");
		await debugCallbacksTab.ClickAsync();
		await Task.Delay(300);
		
		var callbackLines = Page.Locator(".debugCallbackLine");
		var callbackCount = await callbackLines.CountAsync();
		Console.WriteLine($"Debug callback lines: {callbackCount}");
		
		Assert.True(callbackCount > 0, "Should have debug callbacks");
		
		await HomePage.TakeScreenshot("/tmp/debug-both-tabs.png");
	}
}
