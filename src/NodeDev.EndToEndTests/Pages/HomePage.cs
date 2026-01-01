using Microsoft.Playwright;

namespace NodeDev.EndToEndTests.Pages;

public class HomePage
{
	private readonly IPage _user;

	public HomePage(Hooks.Hooks hooks)
	{
		_user = hooks.User;
	}

	private ILocator SearchAppBar => _user.Locator("[data-test-id='appBar']");
	private ILocator SearchNewProjectButton => SearchAppBar.Locator("[data-test-id='newProject']");
	private ILocator SearchProjectExplorer => _user.Locator("[data-test-id='projectExplorer']");
	private ILocator SearchProjectExplorerClasses => SearchProjectExplorer.Locator("[data-test-id='projectExplorerClass'] p");
	private ILocator SearchProjectExplorerTabsHeader => _user.Locator("[data-test-id='ProjectExplorerSection'] .mud-tabs-tabbar");
	private ILocator SearchClassExplorer => _user.Locator("[data-test-id='classExplorer']");
	private ILocator SearchSnackBarContainer => _user.Locator("#mud-snackbar-container");
	private ILocator SearchOptionsButton => SearchAppBar.Locator("[data-test-id='options']");
	private ILocator SearchSaveButton => SearchAppBar.Locator("[data-test-id='save']");
	private ILocator SearchSaveAsButton => SearchAppBar.Locator("[data-test-id='saveAs']");
	private ILocator SearchGraphCanvas => _user.Locator("[data-test-id='graph-canvas']");


	public async Task CreateNewProject()
	{
		await SearchNewProjectButton.WaitForVisible();

		await SearchNewProjectButton.ClickAsync();

		await Task.Delay(100);
	}

	public async Task HasClass(string name)
	{
		await SearchProjectExplorerClasses.GetByText(name).WaitForVisible();
	}

	public async Task ClickClass(string name)
	{
		await SearchProjectExplorerClasses.GetByText(name).ClickAsync();
	}

	public async Task OpenProjectExplorerProjectTab()
	{
		await SearchProjectExplorerTabsHeader.GetByText("PROJECT").ClickAsync();

		await Task.Delay(100);
	}

	public async Task OpenProjectExplorerClassTab()
	{
		await SearchProjectExplorerTabsHeader.GetByText("CLASS").ClickAsync();

		await Task.Delay(100);
	}

	public async Task<ILocator> FindMethodByName(string name)
	{
		await OpenProjectExplorerClassTab();

		var locator = SearchClassExplorer.Locator($"[data-test-id='Method'][data-test-method='{name}']");
		return locator;
	}

	public async Task HasMethodByName(string name)
	{
		var locator = await FindMethodByName(name);

		await locator.WaitForVisible();
	}

	public async Task OpenMethod(string name)
	{
		var locator = await FindMethodByName(name);

		await locator.WaitForVisible();
		await locator.ClickAsync();
		
		await Task.Delay(200); // Wait for method to open
	}

	public async Task SaveProject()
	{
		await SearchSaveButton.WaitForVisible();
		await SearchSaveButton.ClickAsync();
	}

	public async Task OpenOptionsDialog()
	{
		await SearchOptionsButton.WaitForVisible();
		await SearchOptionsButton.ClickAsync();
	}

	public async Task SetProjectsDirectory(string directory)
	{
		var projectsDirectoryInput = _user.Locator("[data-test-id='optionsProjectDirectory']");
		await projectsDirectoryInput.WaitForVisible();
		await projectsDirectoryInput.FillAsync(directory);
	}

	public async Task AcceptOptions()
	{
		var acceptButton = _user.Locator("[data-test-id='optionsAccept']");
		await acceptButton.WaitForVisible();
		await acceptButton.ClickAsync();
	}

	public async Task OpenSaveAsDialog()
	{
		await SearchSaveAsButton.WaitForVisible();
		await SearchSaveAsButton.ClickAsync();
	}

	public async Task SetProjectNameAs(string projectName)
	{
		var projectNameInput = _user.Locator("[data-test-id='saveAsProjectName']");
		await projectNameInput.WaitForVisible();
		await projectNameInput.FillAsync(projectName);
	}

	public async Task AcceptSaveAs()
	{
		var saveButton = _user.Locator("[data-test-id='saveAsSave']");
		await saveButton.WaitForVisible();
		await saveButton.ClickAsync();
	}

	public async Task SnackBarHasByText(string text)
	{
		await SearchSnackBarContainer.GetByText(text).WaitForVisible();
	}

	// Graph Node and Connection Methods

	public ILocator GetGraphNode(string nodeName)
	{
		return _user.Locator($"[data-test-id='graph-node'][data-test-node-name='{nodeName}']");
	}

	public async Task<bool> HasGraphNode(string nodeName)
	{
		var node = GetGraphNode(nodeName);
		try
		{
			await node.WaitForVisible();
			return true;
		}
		catch (TimeoutException)
		{
			// Node is not visible within timeout
			return false;
		}
	}

	public async Task DragNodeTo(string nodeName, float targetX, float targetY)
	{
		var node = GetGraphNode(nodeName);
		await node.WaitForVisible();

		// Get the current bounding box of the node
		var box = await node.BoundingBoxAsync();
		if (box == null)
			throw new Exception($"Could not get bounding box for node '{nodeName}'");

		// Calculate center of node as the starting point
		var sourceX = (float)(box.X + box.Width / 2);
		var sourceY = (float)(box.Y + box.Height / 2);

		Console.WriteLine($"Dragging {nodeName} from ({sourceX}, {sourceY}) to ({targetX}, {targetY})");

		// Perform manual drag with proper event sequence for Blazor.Diagrams
		// 1. Move mouse to starting position
		await _user.Mouse.MoveAsync(sourceX, sourceY);
		await Task.Delay(50);
		
		// 2. Press mouse button down (pointerdown event)
		await _user.Mouse.DownAsync();
		await Task.Delay(50);
		
		// 3. Move mouse to target position with multiple steps (pointermove events)
		await _user.Mouse.MoveAsync(targetX, targetY, new() { Steps = 30 });
		await Task.Delay(50);
		
		// 4. Release mouse button (pointerup event)
		await _user.Mouse.UpAsync();
		
		// Wait for the UI to update after drag
		await Task.Delay(300);
	}

	public async Task<(float X, float Y)> GetNodePosition(string nodeName)
	{
		var node = GetGraphNode(nodeName);
		await node.WaitForVisible();

		var box = await node.BoundingBoxAsync();
		if (box == null)
			throw new Exception($"Could not get bounding box for node '{nodeName}'");

		return ((float)box.X, (float)box.Y);
	}

	public ILocator GetGraphPort(string nodeName, string portName, bool isInput)
	{
		var node = GetGraphNode(nodeName);
		var portType = isInput ? "input" : "output";
		// Look for the port by its name within the node's ports
		return node.Locator($".col.{portType}").Filter(new() { HasText = portName }).Locator(".diagram-port").First;
	}

	public async Task ConnectPorts(string sourceNodeName, string sourcePortName, string targetNodeName, string targetPortName)
	{
		Console.WriteLine($"Connecting ports: {sourceNodeName}.{sourcePortName} -> {targetNodeName}.{targetPortName}");
		
		// Get source port (output)
		var sourcePort = GetGraphPort(sourceNodeName, sourcePortName, isInput: false);
		await sourcePort.WaitForVisible();

		// Get target port (input)
		var targetPort = GetGraphPort(targetNodeName, targetPortName, isInput: true);
		await targetPort.WaitForVisible();

		// Get positions
		var sourceBox = await sourcePort.BoundingBoxAsync();
		var targetBox = await targetPort.BoundingBoxAsync();

		if (sourceBox == null || targetBox == null)
			throw new Exception("Could not get bounding boxes for ports");

		// Calculate centers
		var sourceX = (float)(sourceBox.X + sourceBox.Width / 2);
		var sourceY = (float)(sourceBox.Y + sourceBox.Height / 2);
		var targetX = (float)(targetBox.X + targetBox.Width / 2);
		var targetY = (float)(targetBox.Y + targetBox.Height / 2);

		Console.WriteLine($"Port positions: ({sourceX}, {sourceY}) -> ({targetX}, {targetY})");

		// Perform drag from source port to target port using same approach as node dragging
		await _user.Mouse.MoveAsync(sourceX, sourceY);
		await Task.Delay(50);
		await _user.Mouse.DownAsync();
		await Task.Delay(50);
		await _user.Mouse.MoveAsync(targetX, targetY, new() { Steps = 20 });
		await Task.Delay(50);
		await _user.Mouse.UpAsync();
		await Task.Delay(200); // Wait for connection to be established
	}

	public async Task TakeScreenshot(string fileName)
	{
		await _user.ScreenshotAsync(new() { Path = fileName });
	}
}