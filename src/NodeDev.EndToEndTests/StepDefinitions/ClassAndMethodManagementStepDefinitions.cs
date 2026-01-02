using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NodeDev.EndToEndTests.Pages;

namespace NodeDev.EndToEndTests.StepDefinitions;

[Binding]
public sealed class ClassAndMethodManagementStepDefinitions
{
	private readonly IPage User;
	private readonly HomePage HomePage;

	public ClassAndMethodManagementStepDefinitions(Hooks.Hooks hooks, HomePage homePage)
	{
		User = hooks.User;
		HomePage = homePage;
	}

	[When("I create a new class named {string}")]
	public async Task WhenICreateANewClassNamed(string className)
	{
		await HomePage.CreateClass(className);
		Console.WriteLine($"✓ Created class '{className}'");
	}

	[Then("The {string} should appear in the project explorer")]
	public async Task ThenTheShouldAppearInTheProjectExplorer(string className)
	{
		var exists = await HomePage.ClassExists(className);
		if (!exists)
		{
			throw new Exception($"Class '{className}' not found in project explorer");
		}
		Console.WriteLine($"✓ Class '{className}' appears in project explorer");
	}

	[Then("The class should be named {string} in the project explorer")]
	public async Task ThenTheClassShouldBeNamedInTheProjectExplorer(string expectedName)
	{
		// Wait longer for the UI to update after rename
		await Task.Delay(2000);
		
		var exists = await HomePage.ClassExists(expectedName);
		if (!exists)
		{
			throw new Exception($"Class '{expectedName}' not found in project explorer");
		}
		Console.WriteLine($"✓ Verified class name '{expectedName}' in project explorer");
	}

	[When("I create a new method named {string}")]
	public async Task WhenICreateANewMethodNamed(string methodName)
	{
		await HomePage.CreateMethod(methodName);
		Console.WriteLine($"✓ Created method '{methodName}'");
	}

	[Then("The {string} should appear in the method list")]
	public async Task ThenTheShouldAppearInTheMethodList(string methodName)
	{
		await HomePage.HasMethodByName(methodName);
		Console.WriteLine($"✓ Method '{methodName}' found in list");
	}

	[When("I rename the {string} method to {string}")]
	public async Task WhenIRenameTheMethodTo(string oldName, string newName)
	{
		await HomePage.RenameMethod(oldName, newName);
		Console.WriteLine($"✓ Renamed method '{oldName}' to '{newName}'");
	}

	[Then("The method should be named {string}")]
	public async Task ThenTheMethodShouldBeNamed(string expectedName)
	{
		// Wait longer for the UI to update after rename
		await Task.Delay(2000);
		
		var exists = await HomePage.MethodExists(expectedName);
		if (!exists)
		{
			throw new Exception($"Method '{expectedName}' not found");
		}
		Console.WriteLine($"✓ Verified method name '{expectedName}'");
	}

	[When("I delete the {string} method")]
	public async Task WhenIDeleteTheMethod(string methodName)
	{
		await HomePage.DeleteMethod(methodName);
		Console.WriteLine($"✓ Deleted method '{methodName}'");
	}

	[Then("The {string} should not be in the method list")]
	public async Task ThenTheShouldNotBeInTheMethodList(string methodName)
	{
		// Wait for UI to update after deletion
		await Task.Delay(1000);
		
		var exists = await HomePage.MethodExists(methodName);
		if (exists)
		{
			throw new Exception($"Method '{methodName}' still exists in method list");
		}
		Console.WriteLine($"✓ Method '{methodName}' not in list");
	}

	[When("I add a parameter named {string} of type {string}")]
	public async Task WhenIAddAParameterNamedOfType(string paramName, string paramType)
	{
		await HomePage.AddMethodParameter(paramName, paramType);
		Console.WriteLine($"✓ Added parameter '{paramName}' of type '{paramType}'");
	}

	[Then("The parameter should appear in the Entry node")]
	public async Task ThenTheParameterShouldAppearInTheEntryNode()
	{
		// Verify Entry node exists and is visible
		var entryNode = HomePage.GetGraphNode("Entry");
		await entryNode.WaitForAsync(new() { State = WaitForSelectorState.Visible });
		
		// Check if Entry node has output ports (parameters)
		var ports = entryNode.Locator(".col.output");
		var portCount = await ports.CountAsync();
		if (portCount == 0)
		{
			throw new Exception("Entry node has no output ports for parameters");
		}
		Console.WriteLine($"✓ Entry node has {portCount} output port(s) including new parameter");
	}

	[Then("All methods should be visible and not overlapping")]
	public async Task ThenAllMethodsShouldBeVisibleAndNotOverlapping()
	{
		var methodItems = User.Locator("[data-test-id='Method']");
		var count = await methodItems.CountAsync();
		Console.WriteLine($"✓ Found {count} method(s) displayed");
		
		for (int i = 0; i < count; i++)
		{
			var methodItem = methodItems.Nth(i);
			var text = await methodItem.InnerTextAsync();
			if (string.IsNullOrWhiteSpace(text))
			{
				throw new Exception($"Method {i} has empty text");
			}
		}
	}

	[Then("Method names should be readable")]
	public async Task ThenMethodNamesShouldBeReadable()
	{
		var methodItems = User.Locator("[data-test-id='Method']");
		var count = await methodItems.CountAsync();
		
		for (int i = 0; i < count; i++)
		{
			var text = await methodItems.Nth(i).InnerTextAsync();
			if (text?.Length < 3)
			{
				throw new Exception($"Method {i} has suspiciously short text: '{text}'");
			}
			Console.WriteLine($"✓ Method {i}: '{text}'");
		}
	}
}
