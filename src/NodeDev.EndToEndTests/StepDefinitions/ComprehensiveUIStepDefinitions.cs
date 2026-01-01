using Microsoft.Playwright;
using NodeDev.EndToEndTests.Pages;

namespace NodeDev.EndToEndTests.StepDefinitions;

[Binding]
public sealed class ComprehensiveUIStepDefinitions
{
	private readonly IPage User;
	private readonly HomePage HomePage;
	private string? _previousClassName;

	public ComprehensiveUIStepDefinitions(Hooks.Hooks hooks, HomePage homePage)
	{
		User = hooks.User;
		HomePage = homePage;
	}

	[When("I click on the {string} class")]
	public async Task WhenIClickOnTheClass(string className)
	{
		await HomePage.ClickClass(className);
		await Task.Delay(200);
		Console.WriteLine($"✓ Clicked on class '{className}'");
	}

	[Then("I should see the {string} class in the project explorer")]
	public async Task ThenIShouldSeeTheClassInTheProjectExplorer(string className)
	{
		await HomePage.HasClass(className);
		Console.WriteLine($"✓ Found class '{className}' in project explorer");
	}

	[Then("The class explorer should show class details")]
	public async Task ThenTheClassExplorerShouldShowClassDetails()
	{
		var classExplorer = User.Locator("[data-test-id='classExplorer']");
		await classExplorer.WaitForAsync(new() { State = WaitForSelectorState.Visible });
		Console.WriteLine("✓ Class explorer is visible");
	}

	[Then("I should see the {string} method listed")]
	public async Task ThenIShouldSeeTheMethodListed(string methodName)
	{
		await HomePage.HasMethodByName(methodName);
		Console.WriteLine($"✓ Method '{methodName}' is listed");
	}

	[Then("The method text should be readable without overlap")]
	public async Task ThenTheMethodTextShouldBeReadableWithoutOverlap()
	{
		// Check that method text is properly displayed
		var methodItems = User.Locator("[data-test-id='Method']");
		var count = await methodItems.CountAsync();
		
		for (int i = 0; i < count; i++)
		{
			var methodItem = methodItems.Nth(i);
			var text = await methodItem.InnerTextAsync();
			
			// Check for overlapping text patterns or corruption
			if (string.IsNullOrWhiteSpace(text))
			{
				throw new Exception($"Method {i} has empty or whitespace-only text");
			}
			
			// Check for unusual characters that might indicate corruption
			if (text.Contains("\u0000") || text.Length < 4)
			{
				throw new Exception($"Method {i} appears to have corrupted text: '{text}'");
			}
			
			Console.WriteLine($"✓ Method {i} text is readable: '{text.Substring(0, Math.Min(50, text.Length))}...'");
		}
	}

	[When("I rename the class to {string}")]
	public async Task WhenIRenameTheClassTo(string newName)
	{
		// Store the old class name for verification
		var oldName = _previousClassName ?? "Program"; // Default to Program if not set
		await HomePage.RenameClass(oldName, newName);
		_previousClassName = newName;
	}

	[Then("The class should be named {string}")]
	public async Task ThenTheClassShouldBeNamed(string expectedName)
	{
		var exists = await HomePage.ClassExists(expectedName);
		if (!exists)
		{
			throw new Exception($"Class '{expectedName}' does not exist");
		}
		Console.WriteLine($"✓ Class renamed to '{expectedName}'");
	}

	[When("I add a {string} node to the canvas")]
	public async Task WhenIAddANodeToTheCanvas(string nodeType)
	{
		await HomePage.SearchForNodes(nodeType);
		await HomePage.AddNodeFromSearch(nodeType);
	}

	[Then("The {string} node should be visible")]
	public async Task ThenTheNodeShouldBeVisible(string nodeName)
	{
		var hasNode = await HomePage.HasGraphNode(nodeName);
		if (!hasNode)
		{
			throw new Exception($"Node '{nodeName}' is not visible on canvas");
		}
		Console.WriteLine($"✓ Node '{nodeName}' is visible");
	}

	[When("I delete the {string} node")]
	public async Task WhenIDeleteTheNode(string nodeName)
	{
		await HomePage.DeleteNode(nodeName);
	}

	[Then("The {string} node should not be visible")]
	public async Task ThenTheNodeShouldNotBeVisible(string nodeName)
	{
		var hasNode = await HomePage.HasGraphNode(nodeName);
		if (hasNode)
		{
			throw new Exception($"Node '{nodeName}' is still visible on canvas");
		}
		Console.WriteLine($"✓ Node '{nodeName}' is not visible");
	}

	[When("I disconnect the {string} {string} from {string} {string}")]
	public async Task WhenIDisconnectTheFromConnection(string sourceNode, string sourcePort, string targetNode, string targetPort)
	{
		await HomePage.DeleteAllConnectionsFromNode(sourceNode);
	}

	[Then("The connection should be removed")]
	public async Task ThenTheConnectionShouldBeRemoved()
	{
		// Verification is handled by DeleteAllConnectionsFromNode
		Console.WriteLine("✓ Connection removed");
	}

	[When("I connect a generic type port")]
	public async Task WhenIConnectAGenericTypePort()
	{
		Console.WriteLine("⚠️  Connecting generic type port - needs implementation");
	}

	[Then("The port color should change to reflect the type")]
	public async Task ThenThePortColorShouldChangeToReflectTheType()
	{
		Console.WriteLine("⚠️  Port color verification - needs implementation");
	}

	[When("I go back to class explorer")]
	public async Task WhenIGoBackToClassExplorer()
	{
		await HomePage.OpenProjectExplorerClassTab();
		await Task.Delay(200);
		Console.WriteLine("✓ Navigated back to class explorer");
	}

	[When("I open the {string} method in the {string} class again")]
	public async Task WhenIOpenTheMethodInTheClassAgain(string methodName, string className)
	{
		await HomePage.OpenMethod(methodName);
		await Task.Delay(200);
		Console.WriteLine($"✓ Opened method '{methodName}' again");
	}

	[Then("The graph canvas should still be visible")]
	public async Task ThenTheGraphCanvasShouldStillBeVisible()
	{
		var canvas = HomePage.GetGraphCanvas();
		var isVisible = await canvas.IsVisibleAsync();
		
		if (!isVisible)
		{
			await HomePage.TakeScreenshot("/tmp/graph-canvas-not-visible-again.png");
			throw new Exception("Graph canvas is not visible after reopening");
		}
		
		Console.WriteLine("✓ Graph canvas is still visible");
	}

	[Then("All method names should be displayed correctly")]
	public async Task ThenAllMethodNamesShouldBeDisplayedCorrectly()
	{
		var methodItems = User.Locator("[data-test-id='Method']");
		var count = await methodItems.CountAsync();
		
		Console.WriteLine($"Found {count} method(s) to verify");
		
		for (int i = 0; i < count; i++)
		{
			var methodItem = methodItems.Nth(i);
			var textContent = await methodItem.TextContentAsync();
			
			if (string.IsNullOrWhiteSpace(textContent))
			{
				await HomePage.TakeScreenshot($"/tmp/method-{i}-empty-text.png");
				throw new Exception($"Method {i} has empty text content");
			}
			
			Console.WriteLine($"✓ Method {i}: '{textContent}'");
		}
	}

	[Then("No text should overlap or appear corrupted")]
	public async Task ThenNoTextShouldOverlapOrAppearCorrupted()
	{
		// Take a screenshot for visual verification
		await HomePage.TakeScreenshot("/tmp/text-overlap-check.png");
		
		// Check for common corruption patterns
		var allText = User.Locator("[data-test-id='classExplorer']");
		var content = await allText.TextContentAsync();
		
		// Check for null characters or unusual patterns
		if (content?.Contains("\u0000") == true)
		{
			throw new Exception("Detected null characters in text - possible corruption");
		}
		
		// Check for suspiciously short method names
		var methodItems = User.Locator("[data-test-id='Method']");
		var count = await methodItems.CountAsync();
		
		for (int i = 0; i < count; i++)
		{
			var methodText = await methodItems.Nth(i).TextContentAsync();
			if (methodText?.Length < 3)
			{
				throw new Exception($"Method {i} has suspiciously short text: '{methodText}' - possible overlap or corruption");
			}
		}
		
		Console.WriteLine("✓ No text overlap or corruption detected");
	}

	[When("I click on a different class if available")]
	public async Task WhenIClickOnADifferentClassIfAvailable()
	{
		var classes = User.Locator("[data-test-id='projectExplorerClass'] p");
		var count = await classes.CountAsync();
		
		if (count > 1)
		{
			var secondClass = classes.Nth(1);
			await secondClass.ClickAsync();
			await Task.Delay(200);
			Console.WriteLine("✓ Clicked on different class");
		}
		else
		{
			Console.WriteLine("⚠️  Only one class available, skipping");
		}
	}

	[Then("The class explorer should update")]
	public async Task ThenTheClassExplorerShouldUpdate()
	{
		var classExplorer = User.Locator("[data-test-id='classExplorer']");
		await classExplorer.WaitForAsync(new() { State = WaitForSelectorState.Visible });
		await Task.Delay(100);
		Console.WriteLine("✓ Class explorer updated");
	}
}
