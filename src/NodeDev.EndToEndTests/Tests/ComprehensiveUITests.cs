using NodeDev.EndToEndTests.Fixtures;
using Microsoft.Playwright;
using Xunit;

namespace NodeDev.EndToEndTests.Tests;

public class ComprehensiveUITests : E2ETestBase
{
	public ComprehensiveUITests(AppServerFixture app, PlaywrightFixture playwright)
		: base(app, playwright)
	{
	}

	[Fact(Timeout = 60_000)]
	public async Task TestMethodListingAndTextDisplay()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.ClickClass("Program");
		await HomePage.OpenProjectExplorerClassTab();
		
		// Verify method is listed
		await HomePage.HasMethodByName("Main");
		
		// Check that method text is properly displayed
		var methodItems = Page.Locator("[data-test-id='Method']");
		var count = await methodItems.CountAsync();
		
		for (int i = 0; i < count; i++)
		{
			var methodItem = methodItems.Nth(i);
			var text = await methodItem.InnerTextAsync();
			
			Assert.False(string.IsNullOrWhiteSpace(text), $"Method {i} has empty or whitespace-only text");
			Assert.False(text.Contains("\u0000") || text.Length < 4, $"Method {i} appears to have corrupted text: '{text}'");
			
			Console.WriteLine($"✓ Method {i} text is readable: '{text.Substring(0, Math.Min(50, text.Length))}...'");
		}
		
		await HomePage.TakeScreenshot("/tmp/method-list-display.png");
	}

	[Fact(Timeout = 60_000)]
	public async Task TestDeletingConnections()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");
		
		await HomePage.TakeScreenshot("/tmp/before-disconnect.png");
		
		// Disconnect the nodes
		await HomePage.DeleteConnection("Entry", "Exec", "Return", "Exec");
		
		await HomePage.TakeScreenshot("/tmp/after-disconnect.png");
		Console.WriteLine("✓ Connection removed");
	}

	[Fact(Timeout = 60_000)]
	public async Task TestOpeningMultipleMethods()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");
		
		// Verify graph canvas is visible
		var canvas = HomePage.GetGraphCanvas();
		var isVisible = await canvas.IsVisibleAsync();
		Assert.True(isVisible, "Graph canvas should be visible");
		
		// Go back to class explorer
		await HomePage.OpenProjectExplorerClassTab();
		
		// Open the method again
		await HomePage.OpenMethod("Main");
		
		// Verify graph canvas is still visible
		isVisible = await canvas.IsVisibleAsync();
		Assert.True(isVisible, "Graph canvas should still be visible");
		
		await HomePage.TakeScreenshot("/tmp/multiple-method-opens.png");
	}


	[Fact(Timeout = 60_000)]
	public async Task TestConsoleErrorsDuringAllOperations()
	{
		await HomePage.CreateNewProject();
		
		SetupConsoleMonitoring();
		
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");
		
		// Drag a node
		await DragNodeBy("Return", 100, 50);
		
		AssertNoConsoleErrors();
		await HomePage.TakeScreenshot("/tmp/operations-no-errors.png");
	}

	// Helper method
	private async Task DragNodeBy(string nodeName, int deltaX, int deltaY)
	{
		var currentBox = await HomePage.GetGraphNode(nodeName).BoundingBoxAsync();
		if (currentBox == null)
			throw new Exception($"Could not get bounding box for {nodeName}");

		var targetX = currentBox.X + currentBox.Width / 2 + deltaX;
		var targetY = currentBox.Y + currentBox.Height / 2 + deltaY;

		await HomePage.DragNodeTo(nodeName, (float)targetX, (float)targetY);
	}
}
