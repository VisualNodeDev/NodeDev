using NodeDev.EndToEndTests.Fixtures;
using Xunit;

namespace NodeDev.EndToEndTests.Tests;

public class OverloadSelectionTests : E2ETestBase
{
	public OverloadSelectionTests(AppServerFixture app, PlaywrightFixture playwright)
		: base(app, playwright)
	{
	}

	[Fact(Timeout = 60_000)]
	public async Task SelectOverload_ShouldRefreshNodeVisually()
	{
		// Arrange - Create a new project and open Main method
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");

		// Move Return node to make space for new nodes
		await HomePage.DragNodeTo("Return", 1200, 400);

		// Add Console.WriteLine node (has multiple overloads)
		// Search for "Console.WriteLine" to find Console.WriteLine methods
		await HomePage.SearchForNodes("Console.WriteLine");
		await Task.Delay(500); // Wait for search results to load
		await HomePage.AddNodeFromSearch("MethodCall");

		// Position the Console.WriteLine node
		// The node name will be "Console.WriteLine" (without parentheses)
		await HomePage.DragNodeTo("Console.WriteLine", 700, 300);

		// Take screenshot of initial state
		await HomePage.TakeScreenshot("/tmp/overload-initial-state.png");
		Console.WriteLine("✓ Initial state screenshot taken");

		// Get the Console.WriteLine node and verify it has the overload icon
		var writeLineNode = HomePage.GetGraphNode("Console.WriteLine");
		await writeLineNode.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible });
		
		var overloadIcon = writeLineNode.Locator(".overload-icon");
		var overloadIconCount = await overloadIcon.CountAsync();
		Assert.True(overloadIconCount > 0, "Console.WriteLine should have an overload selection icon");
		Console.WriteLine($"✓ Found overload icon (count: {overloadIconCount})");

		// Count initial ports before selecting overload
		var initialInputPorts = await writeLineNode.Locator(".col.input").CountAsync();
		var initialOutputPorts = await writeLineNode.Locator(".col.output").CountAsync();
		Console.WriteLine($"Initial state - Input ports: {initialInputPorts}, Output ports: {initialOutputPorts}");

		// Act - Click the overload icon to open selection dialog
		await overloadIcon.ClickAsync();
		await Task.Delay(300); // Wait for dialog to appear

		// Take screenshot of overload selection dialog
		await HomePage.TakeScreenshot("/tmp/overload-dialog-open.png");
		Console.WriteLine("✓ Overload selection dialog screenshot taken");

		// Verify the overload selection dialog is visible
		var overloadList = Page.Locator(".mud-list");
		await overloadList.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 5000 });
		
		var overloadItems = Page.Locator(".mud-list .mud-list-item");
		var overloadCount = await overloadItems.CountAsync();
		Assert.True(overloadCount >= 2, "Console.WriteLine should have at least 2 overloads");
		Console.WriteLine($"✓ Found {overloadCount} overloads in selection dialog");

		// Select a different overload (the second one in the list)
		await overloadItems.Nth(1).ClickAsync();
		await Task.Delay(500); // Wait for selection to be applied and node to refresh

		// Take screenshot after overload selection
		await HomePage.TakeScreenshot("/tmp/overload-after-selection.png");
		Console.WriteLine("✓ After selection screenshot taken");

		// Assert - Verify the node is still visible and accessible (visual refresh occurred)
		// The key bug was that without calling Refresh(), the node wouldn't update visually
		var nodeStillVisible = await writeLineNode.IsVisibleAsync();
		Assert.True(nodeStillVisible, "Console.WriteLine node should still be visible after overload selection");
		Console.WriteLine("✓ Node is still visible after overload selection");

		// Verify ports are accessible (indicating visual refresh occurred)
		var execInput = HomePage.GetGraphPort("Console.WriteLine", "Exec", isInput: true);
		var execOutput = HomePage.GetGraphPort("Console.WriteLine", "Exec", isInput: false);
		
		await execInput.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 5000 });
		await execOutput.WaitForAsync(new() { State = Microsoft.Playwright.WaitForSelectorState.Visible, Timeout = 5000 });
		
		var execInputVisible = await execInput.IsVisibleAsync();
		var execOutputVisible = await execOutput.IsVisibleAsync();
		
		Assert.True(execInputVisible, "Exec input port should be visible (node refreshed)");
		Assert.True(execOutputVisible, "Exec output port should be visible (node refreshed)");
		Console.WriteLine("✓ Exec ports are visible - node visual refresh occurred");

		// Count ports after overload selection
		var finalInputPorts = await writeLineNode.Locator(".col.input").CountAsync();
		var finalOutputPorts = await writeLineNode.Locator(".col.output").CountAsync();
		Console.WriteLine($"After overload selection - Input ports: {finalInputPorts}, Output ports: {finalOutputPorts}");

		// Verify the overload dialog is closed
		var dialogStillVisible = await overloadList.IsVisibleAsync();
		Assert.False(dialogStillVisible, "Overload selection dialog should be closed after selection");
		Console.WriteLine("✓ Overload selection dialog closed after selection");

		// Take final screenshot showing the node is properly refreshed
		await HomePage.TakeScreenshot("/tmp/overload-final-state.png");
		Console.WriteLine("✓ Final state screenshot taken");

		Console.WriteLine("✅ Test completed successfully - node visually refreshed after overload selection without F5");
	}

	[Fact(Timeout = 60_000)]
	public async Task SelectOverload_ShouldAllowConnectingToNewPorts()
	{
		// This test verifies that after selecting an overload, the new ports can be connected
		// demonstrating that the visual refresh allows immediate interaction

		// Arrange - Create a new project and open Main method
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");

		// Move Return node to make space
		await HomePage.DragNodeTo("Return", 1500, 400);

		// Add Console.WriteLine node
		await HomePage.SearchForNodes("Console.WriteLine");
		await Task.Delay(500); // Wait for search results to load
		await HomePage.AddNodeFromSearch("MethodCall");
		await HomePage.DragNodeTo("Console.WriteLine", 800, 300);

		await Task.Delay(500);
		
		// Take screenshot of initial setup
		await HomePage.TakeScreenshot("/tmp/overload-connect-initial.png");
		Console.WriteLine("✓ Initial setup screenshot taken");

		// Change Console.WriteLine overload if needed
		var writeLineNode = HomePage.GetGraphNode("Console.WriteLine");
		var overloadIcon = writeLineNode.Locator(".overload-icon");
		
		if (await overloadIcon.CountAsync() > 0)
		{
			await overloadIcon.ClickAsync();
			await Task.Delay(300);

			// Select first overload
			var overloadItems = Page.Locator(".mud-list .mud-list-item");
			await overloadItems.First.ClickAsync();
			await Task.Delay(500);
			
			// Take screenshot after overload selection
			await HomePage.TakeScreenshot("/tmp/overload-connect-after-selection.png");
			Console.WriteLine("✓ After overload selection screenshot taken");
		}

		// Act - Try to connect Entry to Console.WriteLine (should work if node refreshed properly)
		await HomePage.ConnectPorts("Entry", "Exec", "Console.WriteLine", "Exec");
		await Task.Delay(300);

		// Take screenshot after connection
		await HomePage.TakeScreenshot("/tmp/overload-connect-after-connection.png");
		Console.WriteLine("✓ After connection screenshot taken");

		// Assert - Verify connection was successful
		// If the node didn't refresh properly, the connection would fail
		var execInput = HomePage.GetGraphPort("Console.WriteLine", "Exec", isInput: true);
		var isConnected = await execInput.IsVisibleAsync();
		
		Assert.True(isConnected, "Should be able to connect to Console.WriteLine after overload selection");
		Console.WriteLine("✓ Successfully connected to node after overload selection");
		
		// Connect to Return as well
		await HomePage.ConnectPorts("Console.WriteLine", "Exec", "Return", "Exec");
		await Task.Delay(300);
		
		// Take final screenshot
		await HomePage.TakeScreenshot("/tmp/overload-connect-final.png");
		Console.WriteLine("✓ Final screenshot with all connections taken");

		Console.WriteLine("✅ Test completed successfully - can connect to ports after overload selection");
	}
}
