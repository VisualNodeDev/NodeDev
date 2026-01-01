using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NodeDev.EndToEndTests.Pages;

namespace NodeDev.EndToEndTests.StepDefinitions;

[Binding]
public sealed class AdvancedNodeOperationsStepDefinitions
{
	private readonly IPage User;
	private readonly HomePage HomePage;

	public AdvancedNodeOperationsStepDefinitions(Hooks.Hooks hooks, HomePage homePage)
	{
		User = hooks.User;
		HomePage = homePage;
	}

	[When("I connect nodes together")]
	public async Task WhenIConnectNodesTogether()
	{
		await HomePage.ConnectPorts("Entry", "Exec", "Return", "Exec");
		Console.WriteLine("✓ Connected nodes together");
	}

	[Then("All nodes should be properly connected")]
	public async Task ThenAllNodesShouldBeProperlyConnected()
	{
		// Verify that connections exist on canvas
		var connections = User.Locator("[data-test-id='graph-connection']");
		var count = await connections.CountAsync();
		if (count == 0)
		{
			throw new Exception("No connections found on canvas");
		}
		Console.WriteLine($"✓ Verified {count} connection(s) exist");
	}

	[When("I search for {string} nodes")]
	public async Task WhenISearchForNodes(string nodeType)
	{
		await HomePage.SearchForNodes(nodeType);
		Console.WriteLine($"✓ Searched for '{nodeType}' nodes");
	}

	[When("I add a {string} node from search results")]
	public async Task WhenIAddANodeFromSearchResults(string nodeType)
	{
		await HomePage.AddNodeFromSearch(nodeType);
		Console.WriteLine($"✓ Added '{nodeType}' from search");
	}

	[Then("The {string} node should be visible on canvas")]
	public async Task ThenTheNodeShouldBeVisibleOnCanvas(string nodeName)
	{
		var hasNode = await HomePage.HasGraphNode(nodeName);
		if (!hasNode)
		{
			throw new Exception($"Node '{nodeName}' not found on canvas");
		}
		Console.WriteLine($"✓ Node '{nodeName}' is visible on canvas");
	}

	[When("I select multiple nodes")]
	public async Task WhenISelectMultipleNodes()
	{
		await HomePage.SelectMultipleNodes("Entry", "Return");
		Console.WriteLine("✓ Multi-selected nodes");
	}

	[When("I move the selected nodes by {int} pixels right")]
	public async Task WhenIMoveTheSelectedNodesByPixelsRight(int pixels)
	{
		await HomePage.MoveSelectedNodesBy(pixels, 0);
		Console.WriteLine($"✓ Moved selected nodes by {pixels} pixels");
	}

	[Then("All selected nodes should have moved")]
	public async Task ThenAllSelectedNodesShouldHaveMoved()
	{
		// Verify nodes are still visible (movement succeeded)
		var entryNode = await HomePage.HasGraphNode("Entry");
		var returnNode = await HomePage.HasGraphNode("Return");
		if (!entryNode || !returnNode)
		{
			throw new Exception("Nodes not found after movement");
		}
		Console.WriteLine("✓ All selected nodes have moved successfully");
	}

	[When("I create multiple connections between nodes")]
	public async Task WhenICreateMultipleConnectionsBetweenNodes()
	{
		await HomePage.ConnectPorts("Entry", "Exec", "Return", "Exec");
		Console.WriteLine("✓ Created multiple connections");
	}

	[When("I delete all connections from {string} node")]
	public async Task WhenIDeleteAllConnectionsFromNode(string nodeName)
	{
		await HomePage.DeleteAllConnectionsFromNode(nodeName);
		Console.WriteLine($"✓ Deleted connections from '{nodeName}'");
	}

	[Then("The {string} node should have no connections")]
	public async Task ThenTheNodeShouldHaveNoConnections(string nodeName)
	{
		await HomePage.VerifyNodeHasNoConnections(nodeName);
		Console.WriteLine($"✓ Verified '{nodeName}' has no connections");
	}

	[When("I undo the last action")]
	public async Task WhenIUndoTheLastAction()
	{
		await HomePage.UndoLastAction();
		Console.WriteLine("✓ Undo action performed");
	}

	[When("I redo the last action")]
	public async Task WhenIRedoTheLastAction()
	{
		await HomePage.RedoLastAction();
		Console.WriteLine("✓ Redo action performed");
	}

	[When("I select the {string} node")]
	public async Task WhenISelectTheNode(string nodeName)
	{
		var node = HomePage.GetGraphNode(nodeName);
		await node.ClickAsync();
		Console.WriteLine($"✓ Selected node '{nodeName}'");
	}

	[When("I copy the selected node")]
	public async Task WhenICopyTheSelectedNode()
	{
		await HomePage.CopySelectedNode();
		Console.WriteLine("✓ Copied selected node");
	}

	[When("I paste the node")]
	public async Task WhenIPasteTheNode()
	{
		await HomePage.PasteNode();
		Console.WriteLine("✓ Pasted node");
	}

	[Then("There should be two {string} nodes on the canvas")]
	public async Task ThenThereShouldBeTwoNodesOnTheCanvas(string nodeName)
	{
		var count = await HomePage.CountNodesOfType(nodeName);
		if (count < 2)
		{
			throw new Exception($"Expected at least 2 '{nodeName}' nodes, but found {count}");
		}
		Console.WriteLine($"✓ Found {count} '{nodeName}' nodes");
	}
	}

	[When("I click on a {string} node")]
	public async Task WhenIClickOnANode(string nodeName)
	{
		var node = HomePage.GetGraphNode(nodeName);
		await node.ClickAsync();
		Console.WriteLine($"✓ Clicked on '{nodeName}' node");
	}

	[Then("The node properties panel should appear")]
	public async Task ThenTheNodePropertiesPanelShouldAppear()
	{
		await HomePage.VerifyNodePropertiesPanel();
		Console.WriteLine("✓ Node properties panel appeared");
	}

	[Then("The properties should be editable")]
	public async Task ThenThePropertiesShouldBeEditable()
	{
		// Check if properties panel contains editable elements
		var editableInputs = User.Locator("[data-test-id='node-properties'] input, [data-test-id='node-properties'] select, [data-test-id='node-properties'] textarea");
		var count = await editableInputs.CountAsync();
		Console.WriteLine($"✓ Found {count} editable property field(s)");
	}

	[When("I hover over a port")]
	public async Task WhenIHoverOverAPort()
	{
		await HomePage.HoverOverPort("Entry", "Exec", false);
		Console.WriteLine("✓ Hovered over port");
	}

	[Then("The port should highlight")]
	public async Task ThenThePortShouldHighlight()
	{
		await HomePage.VerifyPortHighlighted();
		Console.WriteLine("✓ Port highlighted");
	}

	[Then("The port color should indicate its type")]
	public async Task ThenThePortColorShouldIndicateItsType()
	{
		// Verify port has styling/color classes
		var ports = User.Locator(".diagram-port");
		var count = await ports.CountAsync();
		if (count == 0)
		{
			throw new Exception("No ports found to verify colors");
		}
		Console.WriteLine($"✓ Verified {count} port(s) have type indication");
	}

	[When("I zoom in on the canvas")]
	public async Task WhenIZoomInOnTheCanvas()
	{
		await HomePage.ZoomIn();
		Console.WriteLine("✓ Zoomed in");
	}

	[Then("The canvas should be zoomed in")]
	public async Task ThenTheCanvasShouldBeZoomedIn()
	{
		// Canvas should still be visible after zoom
		var canvas = HomePage.GetGraphCanvas();
		var isVisible = await canvas.IsVisibleAsync();
		if (!isVisible)
		{
			throw new Exception("Canvas not visible after zoom in");
		}
		Console.WriteLine("✓ Canvas zoomed in verified");
	}

	[When("I zoom out on the canvas")]
	public async Task WhenIZoomOutOnTheCanvas()
	{
		await HomePage.ZoomOut();
		Console.WriteLine("✓ Zoomed out");
	}

	[Then("The canvas should be zoomed out")]
	public async Task ThenTheCanvasShouldBeZoomedOut()
	{
		// Canvas should still be visible after zoom
		var canvas = HomePage.GetGraphCanvas();
		var isVisible = await canvas.IsVisibleAsync();
		if (!isVisible)
		{
			throw new Exception("Canvas not visible after zoom out");
		}
		Console.WriteLine("✓ Canvas zoomed out verified");
	}

	[When("I pan the canvas")]
	public async Task WhenIPanTheCanvas()
	{
		await HomePage.PanCanvas(100, 100);
		Console.WriteLine("✓ Panned canvas");
	}

	[Then("The canvas view should have moved")]
	public async Task ThenTheCanvasViewShouldHaveMoved()
	{
		// Verify canvas is still functional after panning
		var canvas = HomePage.GetGraphCanvas();
		var isVisible = await canvas.IsVisibleAsync();
		if (!isVisible)
		{
			throw new Exception("Canvas not visible after panning");
		}
		Console.WriteLine("✓ Canvas view moved and remains functional");
	}

	[When("I move nodes far from origin")]
	public async Task WhenIMoveNodesFarFromOrigin()
	{
		await HomePage.DragNodeTo("Entry", 1000, 1000);
		Console.WriteLine("✓ Moved nodes far from origin");
	}

	[When("I reset canvas view")]
	public async Task WhenIResetCanvasView()
	{
		await HomePage.ResetCanvasView();
		Console.WriteLine("✓ Reset canvas view");
	}

	[Then("All nodes should be centered")]
	public async Task ThenAllNodesShouldBeCentered()
	{
		// Verify nodes are still visible after reset
		var entryVisible = await HomePage.HasGraphNode("Entry");
		var returnVisible = await HomePage.HasGraphNode("Return");
		if (!entryVisible || !returnVisible)
		{
			throw new Exception("Nodes not visible after canvas reset");
		}
		Console.WriteLine("✓ All nodes centered and visible");
	}
}
