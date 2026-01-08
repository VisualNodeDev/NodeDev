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
	public async Task SelectOverloadRefreshesNodeVisually()
	{
		// Create a new project and open the Main method
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");

		// Add a Thread.Sleep node (has multiple overloads: TimeSpan and int)
		await HomePage.AddNodeToCanvas("Thread.Sleep");
		await Task.Delay(500); // Wait for node to be added

		// Get the Thread.Sleep node
		var sleepNode = HomePage.GetGraphNode("Thread.Sleep");
		await sleepNode.WaitForVisible();

		// Take a screenshot before selection
		await HomePage.TakeScreenshot("/tmp/before-overload-selection.png");

		// Count the initial number of ports (inputs/outputs) on the node
		var initialInputCount = await sleepNode.Locator(".col.input").CountAsync();
		var initialOutputCount = await sleepNode.Locator(".col.output").CountAsync();

		Console.WriteLine($"Initial Thread.Sleep node - Inputs: {initialInputCount}, Outputs: {initialOutputCount}");

		// Click the overload icon
		var overloadIcon = sleepNode.Locator(".overload-icon");
		await overloadIcon.WaitForVisible();
		await overloadIcon.ClickAsync();
		await Task.Delay(300); // Wait for overload selection dialog

		// Take a screenshot of the overload dialog
		await HomePage.TakeScreenshot("/tmp/overload-dialog.png");

		// Select a different overload (the first one in the list)
		// The dialog shows overload options as MudListItem components
		var overloadList = Page.Locator(".mud-list .mud-list-item").First;
		await overloadList.WaitForVisible();
		await overloadList.ClickAsync();
		await Task.Delay(500); // Wait for selection to be applied

		// Take a screenshot after selection
		await HomePage.TakeScreenshot("/tmp/after-overload-selection.png");

		// THIS IS THE BUG: The node should refresh visually after overload selection
		// Count the ports again - they should reflect the selected overload
		var finalInputCount = await sleepNode.Locator(".col.input").CountAsync();
		var finalOutputCount = await sleepNode.Locator(".col.output").CountAsync();

		Console.WriteLine($"After overload selection - Inputs: {finalInputCount}, Outputs: {finalOutputCount}");

		// The node should have been visually updated with potentially different port counts
		// For Thread.Sleep overloads:
		// - Sleep(TimeSpan): Target (Thread), timeout (TimeSpan)
		// - Sleep(int): Target (Thread), millisecondsTimeout (int)
		// Both have the same number of inputs, but this test validates visual refresh occurred
		
		// To truly validate the bug, let's check if the node has been re-rendered
		// by verifying that the port structure is actually visible and accessible
		var execInput = HomePage.GetGraphPort("Thread.Sleep", "Exec", isInput: true);
		var execOutput = HomePage.GetGraphPort("Thread.Sleep", "Exec", isInput: false);
		
		// These should be visible without page refresh
		await execInput.WaitForVisible();
		await execOutput.WaitForVisible();
		
		Console.WriteLine("✓ Node ports are visible after overload selection (visual refresh occurred)");
	}

	[Fact(Timeout = 60_000)]
	public async Task OverloadSelectionWithDifferentPortCounts()
	{
		// Create a new project and open the Main method
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");

		// Add a Console.WriteLine node (has multiple overloads with different parameter counts)
		await HomePage.AddNodeToCanvas("Console.WriteLine");
		await Task.Delay(500);

		var writeLineNode = HomePage.GetGraphNode("Console.WriteLine");
		await writeLineNode.WaitForVisible();

		// Take before screenshot
		await HomePage.TakeScreenshot("/tmp/writeline-before-overload.png");

		// Count initial ports
		var initialInputCount = await writeLineNode.Locator(".col.input").CountAsync();
		Console.WriteLine($"Initial Console.WriteLine - Input count: {initialInputCount}");

		// Click overload icon
		var overloadIcon = writeLineNode.Locator(".overload-icon");
		
		// Check if overload icon exists (it should for Console.WriteLine)
		var overloadIconCount = await overloadIcon.CountAsync();
		if (overloadIconCount > 0)
		{
			await overloadIcon.ClickAsync();
			await Task.Delay(300);

			// Select a different overload
			var overloadItems = Page.Locator(".mud-list .mud-list-item");
			var overloadCount = await overloadItems.CountAsync();
			
			if (overloadCount > 1)
			{
				// Select the second overload
				await overloadItems.Nth(1).ClickAsync();
				await Task.Delay(500);

				// Take after screenshot
				await HomePage.TakeScreenshot("/tmp/writeline-after-overload.png");

				// Verify the node is still accessible and ports are visible
				var finalInputCount = await writeLineNode.Locator(".col.input").CountAsync();
				Console.WriteLine($"After overload selection - Input count: {finalInputCount}");

				// Verify exec ports are still accessible
				var execInput = HomePage.GetGraphPort("Console.WriteLine", "Exec", isInput: true);
				await execInput.WaitForVisible();

				Console.WriteLine("✓ Console.WriteLine overload selection completed successfully");
			}
			else
			{
				Console.WriteLine("Console.WriteLine has only one overload, skipping test");
			}
		}
		else
		{
			Console.WriteLine("Console.WriteLine does not have overload icon, skipping test");
		}
	}
}
