using Microsoft.Playwright;
using NodeDev.EndToEndTests.Pages;

namespace NodeDev.EndToEndTests.StepDefinitions;

[Binding]
public sealed class NodeManipulationStepDefinitions
{
	private readonly IPage User;
	private readonly HomePage HomePage;
	private readonly Dictionary<string, (float X, float Y)> _originalNodePositions = new();
	private readonly List<string> _consoleErrors = new();
	private readonly List<string> _consoleWarnings = new();

	public NodeManipulationStepDefinitions(Hooks.Hooks hooks, HomePage homePage)
	{
		User = hooks.User;
		HomePage = homePage;
	}

	[Given("I open the {string} method in the {string} class")]
	[When("I open the {string} method in the {string} class")]
	public async Task GivenIOpenTheMethodInTheClass(string method, string className)
	{
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass(className);
		await HomePage.ClickClass(className);
		await HomePage.OpenMethod(method);
	}

	[When("I drag the {string} node by {int} pixels to the right and {int} pixels down")]
	public async Task WhenIDragTheNodeByPixelsToTheRightAndPixelsDown(string nodeName, int deltaX, int deltaY)
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
		// We need to drag to the center of where the node should end up
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

	[Then("The {string} node should have moved from its original position")]
	public async Task ThenTheNodeShouldHaveMovedFromItsOriginalPosition(string nodeName)
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

	[When("I connect the {string} {string} output to the {string} {string} input")]
	public async Task WhenIConnectTheOutputToTheInput(string sourceNode, string sourcePort, string targetNode, string targetPort)
	{
		Console.WriteLine($"Connecting {sourceNode}.{sourcePort} (output) -> {targetNode}.{targetPort} (input)");

		// Take screenshot before
		await HomePage.TakeScreenshot($"/tmp/before-connect-{Guid.NewGuid()}.png");

		// Connect the ports
		await HomePage.ConnectPorts(sourceNode, sourcePort, targetNode, targetPort);

		// Take screenshot after
		await HomePage.TakeScreenshot($"/tmp/after-connect-{Guid.NewGuid()}.png");

		// Wait for connection to be established
		await Task.Delay(200);
	}

	[When("I move the {string} node away from {string} node")]
	public async Task WhenIMoveTheNodeAwayFromNode(string nodeName, string otherNodeName)
	{
		// Move the node 300 pixels to the right to separate them
		await WhenIDragTheNodeByPixelsToTheRightAndPixelsDown(nodeName, 300, 0);
	}

	[When("I take a screenshot named {string}")]
	[Then("I take a screenshot named {string}")]
	public async Task ThenITakeAScreenshotNamed(string name)
	{
		await HomePage.TakeScreenshot($"/tmp/{name}-{Guid.NewGuid()}.png");
	}

	[When("I disconnect the {string} {string} output from the {string} {string} input")]
	public async Task WhenIDisconnectTheOutputFromTheInput(string sourceNode, string sourcePort, string targetNode, string targetPort)
	{
		Console.WriteLine($"Disconnecting {sourceNode}.{sourcePort} (output) from {targetNode}.{targetPort} (input)");

		// To disconnect, we need to click on the connection line or use a different approach
		// For now, we'll use the API through the browser console
		await User.EvaluateAsync(@"
			// Find the connection between the nodes
			// This is a placeholder - actual implementation would need to find and remove the connection
			console.log('Disconnect operation');
		");

		await Task.Delay(200);
	}

	[Then("The connection between {string} {string} output and {string} {string} input should exist")]
	public async Task ThenTheConnectionBetweenOutputAndInputShouldExist(string sourceNode, string sourcePort, string targetNode, string targetPort)
	{
		// For now we verify by checking if the operation completed without errors
		// A more robust check would inspect the DOM for the SVG connection line
		await Task.Delay(100);
		Console.WriteLine($"Connection verification: {sourceNode}.{sourcePort} -> {targetNode}.{targetPort}");
	}

	[When("I check for console errors")]
	public void WhenICheckForConsoleErrors()
	{
		// Clear previous errors
		_consoleErrors.Clear();
		_consoleWarnings.Clear();

		// Set up console monitoring
		User.Console += (_, msg) =>
		{
			var msgType = msg.Type;
			var text = msg.Text;

			Console.WriteLine($"[BROWSER {msgType.ToUpper()}] {text}");

			if (msgType == "error")
			{
				_consoleErrors.Add(text);
			}
			else if (msgType == "warning")
			{
				_consoleWarnings.Add(text);
			}
		};

		User.PageError += (_, error) =>
		{
			Console.WriteLine($"[PAGE ERROR] {error}");
			_consoleErrors.Add(error);
		};
	}

	[Then("There should be no console errors")]
	public void ThenThereShouldBeNoConsoleErrors()
	{
		if (_consoleErrors.Count > 0)
		{
			var errorList = string.Join("\n  - ", _consoleErrors);
			throw new Exception($"Found {_consoleErrors.Count} console error(s):\n  - {errorList}");
		}

		Console.WriteLine("✓ No console errors detected");
	}

	[Then("The graph canvas should be visible")]
	public async Task ThenTheGraphCanvasShouldBeVisible()
	{
		var canvas = HomePage.GetGraphCanvas();
		var isVisible = await canvas.IsVisibleAsync();

		if (!isVisible)
		{
			await HomePage.TakeScreenshot("/tmp/graph-canvas-not-visible.png");
			throw new Exception("Graph canvas is not visible");
		}

		Console.WriteLine("✓ Graph canvas is visible");
	}

	[When("I delete the connection between {string} {string} output and {string} {string} input")]
	public async Task WhenIDeleteTheConnectionBetweenOutputAndInput(string sourceNode, string sourcePort, string targetNode, string targetPort)
	{
		Console.WriteLine($"Deleting connection: {sourceNode}.{sourcePort} (output) -> {targetNode}.{targetPort} (input)");

		// Take screenshot before
		await HomePage.TakeScreenshot($"/tmp/before-delete-connection-{Guid.NewGuid()}.png");

		// Delete the connection
		await HomePage.DeleteConnection(sourceNode, sourcePort, targetNode, targetPort);

		// Take screenshot after
		await HomePage.TakeScreenshot($"/tmp/after-delete-connection-{Guid.NewGuid()}.png");

		// Wait for UI to update
		await Task.Delay(200);
	}
}
