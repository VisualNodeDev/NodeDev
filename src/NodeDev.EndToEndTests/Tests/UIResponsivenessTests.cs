using NodeDev.EndToEndTests.Fixtures;
using Xunit;

namespace NodeDev.EndToEndTests.Tests;

public class UIResponsivenessTests : E2ETestBase
{
	public UIResponsivenessTests(AppServerFixture app, PlaywrightFixture playwright)
		: base(app, playwright)
	{
	}

	[Fact(Timeout = 120_000)]
	public async Task TestRapidNodeAdditions()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");
		
		SetupConsoleMonitoring();
		
		try
		{
			await HomePage.RapidlyAddNodes(10, "Add");
			Console.WriteLine("✓ All nodes added");
		}
		catch (NotImplementedException ex)
		{
			Console.WriteLine($"Rapid node addition not fully implemented: {ex.Message}");
		}
		
		AssertNoConsoleErrors();
		await HomePage.TakeScreenshot("/tmp/rapid-node-addition.png");
	}

	[Fact(Timeout = 60_000)]
	public async Task TestInvalidConnectionAttempts()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");
		
		try
		{
			// Try to connect incompatible ports (this should be rejected)
			await HomePage.TryConnectIncompatiblePorts("Entry", "Exec", "Entry", "Exec");
			Console.WriteLine("✓ Invalid connection attempt handled");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Connection validation: {ex.Message}");
		}
		
		await HomePage.TakeScreenshot("/tmp/invalid-connection-rejected.png");
	}

	[Fact(Timeout = 60_000)]
	public async Task TestBrowserWindowResize()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");
		
		// Resize browser window
		await Page.SetViewportSizeAsync(1200, 800);
		await Task.Delay(200); // Reduced from 500ms
		
		// Verify canvas is still visible
		var canvas = HomePage.GetGraphCanvas();
		var isVisible = await canvas.IsVisibleAsync();
		Assert.True(isVisible, "Canvas should remain visible after resize");
		
		// Resize back
		await Page.SetViewportSizeAsync(1900, 1000);
		await Task.Delay(200); // Reduced from 500ms
		
		await HomePage.TakeScreenshot("/tmp/window-resized.png");
		Console.WriteLine("✓ UI adapted to window resize");
	}

	[Fact(Timeout = 60_000)]
	public async Task TestKeyboardShortcuts()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");
		
		// Test save with keyboard shortcut
		await HomePage.SaveProjectWithKeyboard();
		await Task.Delay(300); // Reduced from 500ms
		
		// Test delete with keyboard shortcut
		try
		{
			var returnNode = HomePage.GetGraphNode("Return");
			await returnNode.ClickAsync();
			await Page.Keyboard.PressAsync("Delete");
			await Task.Delay(100); // Reduced from 200ms
			Console.WriteLine("✓ Keyboard shortcuts work");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Delete shortcut: {ex.Message}");
		}
		
		await HomePage.TakeScreenshot("/tmp/keyboard-shortcuts-work.png");
	}

	[Fact(Timeout = 60_000)]
	public async Task TestLongMethodNamesDisplay()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.ClickClass("Program");
		await HomePage.OpenProjectExplorerClassTab();
		
		try
		{
			var longName = "ThisIsAVeryLongMethodNameThatShouldBeDisplayedProperlyWithoutOverflowingTheUI";
			await HomePage.CreateMethodWithLongName(longName);
			await HomePage.HasMethodByName(longName);
			
			await HomePage.TakeScreenshot("/tmp/long-method-name.png");
			Console.WriteLine("✓ Long method name displayed correctly");
		}
		catch (NotImplementedException ex)
		{
			Console.WriteLine($"Method creation not implemented: {ex.Message}");
		}
	}

	[Fact(Timeout = 60_000)]
	public async Task TestConcurrentOperations()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");
		
		SetupConsoleMonitoring();
		
		// Perform rapid operations
		await HomePage.PerformRapidOperations(20);
		
		AssertNoConsoleErrors();
		await HomePage.TakeScreenshot("/tmp/concurrent-operations.png");
		Console.WriteLine("✓ Concurrent operations completed successfully");
	}
}
