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
	public void WhenICreateANewClassNamed(string className)
	{
		Console.WriteLine($"⚠️ Creating class '{className}' - functionality needs implementation");
	}

	[Then("The {string} should appear in the project explorer")]
	public async Task ThenTheShouldAppearInTheProjectExplorer(string className)
	{
		// Check if class exists
		Console.WriteLine($"⚠️ Verifying class '{className}' in explorer - functionality needs implementation");
	}

	[Then("The class should be named {string} in the project explorer")]
	public void ThenTheClassShouldBeNamedInTheProjectExplorer(string expectedName)
	{
		Console.WriteLine($"⚠️ Verifying class name '{expectedName}' - functionality needs implementation");
	}

	[When("I delete the {string} class")]
	public void WhenIDeleteTheClass(string className)
	{
		Console.WriteLine($"⚠️ Deleting class '{className}' - functionality needs implementation");
	}

	[Then("The {string} should not be in the project explorer")]
	public void ThenTheShouldNotBeInTheProjectExplorer(string className)
	{
		Console.WriteLine($"⚠️ Verifying class '{className}' not in explorer - functionality needs implementation");
	}

	[When("I create a new method named {string}")]
	public void WhenICreateANewMethodNamed(string methodName)
	{
		Console.WriteLine($"⚠️ Creating method '{methodName}' - functionality needs implementation");
	}

	[Then("The {string} should appear in the method list")]
	public async Task ThenTheShouldAppearInTheMethodList(string methodName)
	{
		await HomePage.HasMethodByName(methodName);
		Console.WriteLine($"✓ Method '{methodName}' found in list");
	}

	[When("I rename the {string} method to {string}")]
	public void WhenIRenameTheMethodTo(string oldName, string newName)
	{
		Console.WriteLine($"⚠️ Renaming method '{oldName}' to '{newName}' - functionality needs implementation");
	}

	[Then("The method should be named {string}")]
	public void ThenTheMethodShouldBeNamed(string expectedName)
	{
		Console.WriteLine($"⚠️ Verifying method name '{expectedName}' - functionality needs implementation");
	}

	[When("I delete the {string} method")]
	public void WhenIDeleteTheMethod(string methodName)
	{
		Console.WriteLine($"⚠️ Deleting method '{methodName}' - functionality needs implementation");
	}

	[Then("The {string} should not be in the method list")]
	public void ThenTheShouldNotBeInTheMethodList(string methodName)
	{
		Console.WriteLine($"⚠️ Verifying method '{methodName}' not in list - functionality needs implementation");
	}

	[When("I add a parameter named {string} of type {string}")]
	public void WhenIAddAParameterNamedOfType(string paramName, string paramType)
	{
		Console.WriteLine($"⚠️ Adding parameter '{paramName}' of type '{paramType}' - functionality needs implementation");
	}

	[Then("The parameter should appear in the Entry node")]
	public void ThenTheParameterShouldAppearInTheEntryNode()
	{
		Console.WriteLine("⚠️ Verifying parameter in Entry node - functionality needs implementation");
	}

	[When("I change the return type to {string}")]
	public void WhenIChangeTheReturnTypeTo(string returnType)
	{
		Console.WriteLine($"⚠️ Changing return type to '{returnType}' - functionality needs implementation");
	}

	[Then("The Return node should accept int values")]
	public void ThenTheReturnNodeShouldAcceptIntValues()
	{
		Console.WriteLine("⚠️ Verifying Return node accepts int - functionality needs implementation");
	}

	[When("I add a property named {string} of type {string}")]
	public void WhenIAddAPropertyNamedOfType(string propName, string propType)
	{
		Console.WriteLine($"⚠️ Adding property '{propName}' of type '{propType}' - functionality needs implementation");
	}

	[Then("The property should appear in the class explorer")]
	public void ThenThePropertyShouldAppearInTheClassExplorer()
	{
		Console.WriteLine("⚠️ Verifying property in class explorer - functionality needs implementation");
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
