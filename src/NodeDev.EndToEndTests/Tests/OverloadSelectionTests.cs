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
		// Default Console.WriteLine() has 1 input (Exec) and 1 output (Exec)
		var initialInputPorts = await writeLineNode.Locator(".col.input").CountAsync();
		var initialOutputPorts = await writeLineNode.Locator(".col.output").CountAsync();
		Console.WriteLine($"Initial state - Input ports: {initialInputPorts}, Output ports: {initialOutputPorts}");
		
		// Verify we start with no parameter input ports (only Exec)
		Assert.Equal(1, initialInputPorts); // Only Exec input
		Console.WriteLine("✓ Initial node has only Exec input (no parameters)");

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

		// Select an overload with a parameter (e.g., Console.WriteLine(string value))
		// Look for an overload that has "string" or "object" in the text
		var selectedOverload = false;
		for (int i = 0; i < overloadCount; i++)
		{
			var itemText = await overloadItems.Nth(i).TextContentAsync();
			if (itemText != null && (itemText.Contains("string") || itemText.Contains("object")) && !itemText.Contains(","))
			{
				Console.WriteLine($"✓ Selecting overload with parameter: {itemText}");
				await overloadItems.Nth(i).ClickAsync();
				selectedOverload = true;
				break;
			}
		}
		
		Assert.True(selectedOverload, "Should find an overload with a parameter");
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

		// Count ports after overload selection - should have MORE input ports now
		var finalInputPorts = await writeLineNode.Locator(".col.input").CountAsync();
		var finalOutputPorts = await writeLineNode.Locator(".col.output").CountAsync();
		Console.WriteLine($"After overload selection - Input ports: {finalInputPorts}, Output ports: {finalOutputPorts}");
		
		// THE KEY TEST: Verify that we now have an additional input port for the parameter
		Assert.True(finalInputPorts > initialInputPorts, 
			$"After selecting overload with parameter, should have more input ports. Initial: {initialInputPorts}, Final: {finalInputPorts}");
		Console.WriteLine($"✓ CRITICAL: New parameter input port appeared! Initial inputs: {initialInputPorts}, Final inputs: {finalInputPorts}");
		
		// Try to find the parameter input port (not Exec)
		var parameterInputs = await writeLineNode.Locator(".col.input").AllAsync();
		var foundParameterPort = false;
		foreach (var port in parameterInputs)
		{
			var portText = await port.TextContentAsync();
			if (portText != null && !portText.Contains("Exec"))
			{
				foundParameterPort = true;
				Console.WriteLine($"✓ Found parameter port with label: {portText}");
				break;
			}
		}
		
		Assert.True(foundParameterPort, "Should find at least one non-Exec parameter port");
		Console.WriteLine("✓ Parameter port is visually present and accessible");

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
	public async Task SelectOverload_ShouldShowNewParameterPorts()
	{
		// This test specifically verifies that NEW parameter ports appear visually
		// after selecting an overload with different parameters

		// Arrange - Create a new project and open Main method
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");

		// Move Return node to make space
		await HomePage.DragNodeTo("Return", 1500, 400);

		// Add Console.WriteLine node - default is WriteLine()
		await HomePage.SearchForNodes("Console.WriteLine");
		await Task.Delay(500);
		await HomePage.AddNodeFromSearch("MethodCall");
		await HomePage.DragNodeTo("Console.WriteLine", 700, 300);

		await Task.Delay(500);
		
		var writeLineNode = HomePage.GetGraphNode("Console.WriteLine");
		
		// Count initial ports
		var initialInputPorts = await writeLineNode.Locator(".col.input").CountAsync();
		Console.WriteLine($"Initial input ports: {initialInputPorts}");
		
		// Take screenshot before
		await HomePage.TakeScreenshot("/tmp/overload-param-before.png");
		Console.WriteLine("✓ Screenshot before overload selection");

		// Open overload dialog
		var overloadIcon = writeLineNode.Locator(".overload-icon");
		await overloadIcon.ClickAsync();
		await Task.Delay(300);

		// Select Console.WriteLine(int value) - this has a parameter
		var overloadItems = Page.Locator(".mud-list .mud-list-item");
		var selectedOverload = false;
		var overloadCount = await overloadItems.CountAsync();
		
		for (int i = 0; i < overloadCount; i++)
		{
			var itemText = await overloadItems.Nth(i).TextContentAsync();
			if (itemText != null && itemText.Contains("int") && !itemText.Contains(","))
			{
				Console.WriteLine($"✓ Selecting overload: {itemText}");
				await overloadItems.Nth(i).ClickAsync();
				selectedOverload = true;
				break;
			}
		}
		
		Assert.True(selectedOverload, "Should find Console.WriteLine(int) overload");
		await Task.Delay(500);
		
		// Take screenshot after selection
		await HomePage.TakeScreenshot("/tmp/overload-param-after.png");
		Console.WriteLine("✓ Screenshot after overload selection");

		// CRITICAL TEST: Count ports again - should have more
		var finalInputPorts = await writeLineNode.Locator(".col.input").CountAsync();
		Console.WriteLine($"Final input ports: {finalInputPorts}");
		
		Assert.True(finalInputPorts > initialInputPorts, 
			$"Parameter port should be visible! Initial: {initialInputPorts}, Final: {finalInputPorts}");
		Console.WriteLine($"✅ SUCCESS: Parameter port appeared! {initialInputPorts} -> {finalInputPorts} input ports");
		
		// Verify the int parameter port is actually there
		var paramPorts = await writeLineNode.Locator(".col.input").AllAsync();
		var foundIntPort = false;
		foreach (var port in paramPorts)
		{
			var portText = await port.TextContentAsync();
			if (portText != null && (portText.Contains("value") || portText.Contains("int")))
			{
				foundIntPort = true;
				Console.WriteLine($"✓ Found int parameter port: {portText}");
				break;
			}
		}
		
		Assert.True(foundIntPort, "Should find the 'value' or 'int' parameter port");
		
		// Take final screenshot
		await HomePage.TakeScreenshot("/tmp/overload-param-final.png");
		Console.WriteLine("✅ Test completed - new parameter ports are visually present after overload selection");
	}
}
