using NodeDev.EndToEndTests.Fixtures;
using Xunit;

namespace NodeDev.EndToEndTests.Tests;

public class NodeManipulationTests : E2ETestBase
{
	private readonly Dictionary<string, (float X, float Y)> _originalNodePositions = new();

	public NodeManipulationTests(AppServerFixture app, PlaywrightFixture playwright)
		: base(app, playwright)
	{
	}

	[Fact(Timeout = 60_000)]
	public async Task MoveReturnNodeOnCanvas()
	{
		// Load default project
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");

		// Drag the Return node by 200 pixels to the right and 100 pixels down
		await DragNode("Return", 200, 100);

		// Verify the node has moved from its original position
		await VerifyNodeMoved("Return");
	}

	[Fact(Timeout = 60_000)]
	public async Task MoveReturnNodeMultipleTimes()
	{
		// Load default project
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");

		// Move the Return node multiple times
		await DragNode("Return", 150, 80);
		await VerifyNodeMoved("Return");

		await DragNode("Return", 150, 80);
		await VerifyNodeMoved("Return");

		await DragNode("Return", -200, -100);
		await VerifyNodeMoved("Return");
	}

	[Fact(Timeout = 60_000)]
	public async Task CreateConnectionBetweenEntryAndReturnNodes()
	{
		// Load default project
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");

		// Move Return node away from Entry node
		await DragNode("Return", 300, 0);
		await HomePage.TakeScreenshot("/tmp/nodes-separated.png");

		// Connect the nodes
		await HomePage.ConnectPorts("Entry", "Exec", "Return", "Exec");
		await HomePage.TakeScreenshot("/tmp/after-connection.png");
	}

	[Fact(Timeout = 60_000)]
	public async Task DisconnectAndReconnectNodes()
	{
		// Load default project
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");

		await HomePage.TakeScreenshot("/tmp/initial-connection.png");

		// Move Return node away from Entry node
		await DragNode("Return", 300, 0);
		await HomePage.TakeScreenshot("/tmp/after-move.png");

		// Reconnect the nodes
		await HomePage.ConnectPorts("Entry", "Exec", "Return", "Exec");
		await HomePage.TakeScreenshot("/tmp/reconnected.png");
	}

	[Fact(Timeout = 60_000)]
	public async Task DeleteConnectionBetweenEntryAndReturnNodes()
	{
		// Load default project
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");

		SetupConsoleMonitoring();

		// Delete the connection
		await HomePage.DeleteConnection("Entry", "Exec", "Return", "Exec");

		AssertNoConsoleErrors();
		await HomePage.TakeScreenshot("/tmp/connection-deleted.png");
	}

	[Fact(Timeout = 60_000)]
	public async Task OpenMethodAndCheckForBrowserErrors()
	{
		// Load default project
		await HomePage.CreateNewProject();

		SetupConsoleMonitoring();

		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");

		AssertNoConsoleErrors();

		// Verify graph canvas is visible
		var canvas = HomePage.GetGraphCanvas();
		var isVisible = await canvas.IsVisibleAsync();
		Assert.True(isVisible, "Graph canvas should be visible");
		Console.WriteLine("✓ Graph canvas is visible");
	}

	// Helper methods
	private async Task DragNode(string nodeName, int deltaX, int deltaY)
	{
		// Take screenshot before
		var beforeGuid = Guid.NewGuid();
		await HomePage.TakeScreenshot($"/tmp/before-drag-{nodeName}-{beforeGuid}.png");

		// Get current position (before this drag operation)
		var currentPos = await HomePage.GetNodePosition(nodeName);

		// Store the very first position as original (for validation at the end)
		if (!_originalNodePositions.ContainsKey(nodeName))
		{
			_originalNodePositions[nodeName] = currentPos;
		}

		Console.WriteLine($"Current position of {nodeName}: ({currentPos.X}, {currentPos.Y})");
		Console.WriteLine($"First/Original position of {nodeName}: ({_originalNodePositions[nodeName].X}, {_originalNodePositions[nodeName].Y})");

		// Calculate new absolute position (current position + delta)
		var currentBox = await HomePage.GetGraphNode(nodeName).BoundingBoxAsync();
		if (currentBox == null)
			throw new Exception($"Could not get bounding box for {nodeName}");

		var targetX = currentBox.X + currentBox.Width / 2 + deltaX;
		var targetY = currentBox.Y + currentBox.Height / 2 + deltaY;

		Console.WriteLine($"Target position (page coords): ({targetX}, {targetY})");

		// Drag the node to the target position
		await HomePage.DragNodeTo(nodeName, (float)targetX, (float)targetY);

		// Take screenshot after for validation
		var afterGuid = Guid.NewGuid();
		await HomePage.TakeScreenshot($"/tmp/after-drag-{nodeName}-{afterGuid}.png");

		// Check position immediately after drag
		var posAfterDrag = await HomePage.GetNodePosition(nodeName);
		Console.WriteLine($"Position after drag: ({posAfterDrag.X}, {posAfterDrag.Y})");
		Console.WriteLine($"Movement delta: ({posAfterDrag.X - currentPos.X}, {posAfterDrag.Y - currentPos.Y})");
	}

	private async Task VerifyNodeMoved(string nodeName)
	{
		if (!_originalNodePositions.ContainsKey(nodeName))
			throw new Exception($"No original position stored for node '{nodeName}'");

		// Get new position
		var newPosition = await HomePage.GetNodePosition(nodeName);

		// Verify position changed (allowing for some tolerance due to grid snapping)
		var deltaX = Math.Abs(newPosition.X - _originalNodePositions[nodeName].X);
		var deltaY = Math.Abs(newPosition.Y - _originalNodePositions[nodeName].Y);

		if (deltaX < 50 && deltaY < 50)
		{
			throw new Exception($"Node '{nodeName}' did not move enough. Original: ({_originalNodePositions[nodeName].X}, {_originalNodePositions[nodeName].Y}), New: ({newPosition.X}, {newPosition.Y})");
		}
	}
}
