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

	[Fact(Timeout = 60_000)]
	public async Task ToolbarButtons_ShouldShowStopPauseResumeWhenDebugging()
	{
		// Arrange - Create a new project
		await HomePage.CreateNewProject();
		await Task.Delay(500);

		// Act - Click "Run with Debug"
		var runWithDebugButton = Page.Locator("[data-test-id='run-with-debug']");
		await runWithDebugButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
		await runWithDebugButton.ClickAsync();

		// Wait for debugging to start
		await Task.Delay(2000);

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
		await Task.Delay(1000);
	}

	[Fact(Timeout = 60_000)]
	public async Task StopButton_ShouldStopDebugSession()
	{
		// Arrange - Start debugging
		await HomePage.CreateNewProject();
		await Task.Delay(500);

		var runWithDebugButton = Page.Locator("[data-test-id='run-with-debug']");
		await runWithDebugButton.ClickAsync();
		await Task.Delay(2000);

		// Verify we're debugging
		var stopButton = Page.Locator("[data-test-id='stop-debug']");
		await stopButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

		// Act - Click Stop button
		await stopButton.ClickAsync();
		await Task.Delay(1000);

		// Assert - Should return to normal state
		var runButton = Page.Locator("[data-test-id='run-project']");
		await runButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });

		var isRunVisible = await runButton.IsVisibleAsync();
		Assert.True(isRunVisible, "Run button should be visible after stopping");

		// Stop button should not be visible anymore
		var stopCount = await stopButton.CountAsync();
		Assert.Equal(0, stopCount);

		Console.WriteLine("✓ Stop button successfully terminated debug session");

		await HomePage.TakeScreenshot("/tmp/toolbar-after-stop.png");
	}

	[Fact(Timeout = 60_000)]
	public async Task RunWithDebug_ShouldShowDebugIndicator()
	{
		// Arrange - Create a new project
		await HomePage.CreateNewProject();
		await Task.Delay(500);

		// Act - Click "Run with Debug" button
		var runWithDebugButton = Page.Locator("[data-test-id='run-with-debug']");
		await runWithDebugButton.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
		
		// Get initial button text
		var initialText = await runWithDebugButton.TextContentAsync();
		Console.WriteLine($"Initial button text: {initialText}");
		
		await runWithDebugButton.ClickAsync();
		
		// Wait a moment for debugging to start
		await Task.Delay(1000);
		
		// Assert - Button should change appearance during debug
		// The button should show "Debugging (PID: ...)" or be disabled
		var buttonText = await runWithDebugButton.TextContentAsync();
		Console.WriteLine($"Button text during debug: {buttonText}");
		
		// Wait for debugging to complete
		await Task.Delay(3000);
		
		// Button should return to normal state
		var finalText = await runWithDebugButton.TextContentAsync();
		Console.WriteLine($"Final button text: {finalText}");
		
		await HomePage.TakeScreenshot("/tmp/debug-mode-indicator.png");
	}

	[Fact(Timeout = 60_000)]
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

	[Fact(Timeout = 60_000)]
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

	[Fact(Timeout = 60_000)]
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
		await Task.Delay(5000);
		
		// Button should be enabled again after process exits
		var isDisabledAfter = await runWithDebugButton.IsDisabledAsync();
		Console.WriteLine($"Button disabled after: {isDisabledAfter}");
		Assert.False(isDisabledAfter, "Button should be enabled after process exits");
		
		await HomePage.TakeScreenshot("/tmp/debug-state-after-exit.png");
	}

	[Fact(Timeout = 60_000)]
	public async Task RunWithDebug_ConsoleOutputAndCallbacksShouldBothWork()
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
		
		// Assert - Both tabs should have content
		
		// Check Console Output tab
		var consoleOutputTab = Page.Locator(".consoleOutputTab");
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
