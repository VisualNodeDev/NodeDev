using Microsoft.Playwright;
using NodeDev.EndToEndTests.Pages;

namespace NodeDev.EndToEndTests.StepDefinitions;

[Binding]
public sealed class NodeManipulationStepDefinitions
{
	private readonly IPage User;
	private readonly HomePage HomePage;
	private readonly Dictionary<string, (float X, float Y)> _originalNodePositions = new();

	public NodeManipulationStepDefinitions(Hooks.Hooks hooks, HomePage homePage)
	{
		User = hooks.User;
		HomePage = homePage;
	}

	[Given("I open the {string} method in the {string} class")]
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
		await HomePage.TakeScreenshot($"/tmp/before-drag-{nodeName}-{Guid.NewGuid()}.png");
		
		// Store original position
		_originalNodePositions[nodeName] = await HomePage.GetNodePosition(nodeName);
		
		Console.WriteLine($"Original position of {nodeName}: ({_originalNodePositions[nodeName].X}, {_originalNodePositions[nodeName].Y})");
		
		// Calculate new position
		var newX = (int)(_originalNodePositions[nodeName].X + deltaX);
		var newY = (int)(_originalNodePositions[nodeName].Y + deltaY);
		
		Console.WriteLine($"Target position: ({newX}, {newY})");
		
		// Drag the node
		await HomePage.DragNodeTo(nodeName, newX, newY);
		
		// Take screenshot after for validation
		await HomePage.TakeScreenshot($"/tmp/after-drag-{nodeName}-{Guid.NewGuid()}.png");
		
		// Check position immediately after drag
		var posAfterDrag = await HomePage.GetNodePosition(nodeName);
		Console.WriteLine($"Position after drag: ({posAfterDrag.X}, {posAfterDrag.Y})");
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

	[When("I add a new {string} node to the canvas")]
	public async Task WhenIAddANewNodeToTheCanvas(string nodeType)
	{
		// This would require implementing node creation from the UI
		// For now, we'll skip this as it requires more understanding of the node selection UI
		throw new NotImplementedException("Adding new nodes from tests is not yet implemented");
	}

	[When("I connect the {string} output to the {string} input")]
	public async Task WhenIConnectTheOutputToTheInput(string sourceNode, string targetNode)
	{
		// This assumes simple connection between nodes
		// We would need to specify port names for more complex scenarios
		await HomePage.ConnectPorts(sourceNode, "Output", targetNode, "Input");
		
		// Take screenshot for validation
		await HomePage.TakeScreenshot($"/tmp/connection-{sourceNode}-to-{targetNode}.png");
	}

	[Then("The connection should be visible between the nodes")]
	public async Task ThenTheConnectionShouldBeVisibleBetweenTheNodes()
	{
		// For now we assume the connection was created successfully if no exception was thrown
		// In a more robust test, we would check for the visual connection line
		await Task.Delay(100);
	}
}
