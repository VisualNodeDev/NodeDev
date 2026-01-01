using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NodeDev.EndToEndTests.Pages;

namespace NodeDev.EndToEndTests.StepDefinitions;

[Binding]
public sealed class ConsoleOutputStepDefinitions
{
	private readonly IPage User;
	private readonly HomePage HomePage;

	public ConsoleOutputStepDefinitions(Hooks.Hooks hooks, HomePage homePage)
	{
		User = hooks.User;
		HomePage = homePage;
	}

	[When("I run the project")]
	public async Task WhenIRunTheProject()
	{
		await HomePage.RunProject();
	}

	[Then("The console panel should become visible")]
	public async Task ThenTheConsolePanelShouldBecomeVisible()
	{
		// Wait a bit for the panel to appear
		await Task.Delay(500);
		
		var isVisible = await HomePage.IsConsolePanelVisible();
		if (!isVisible)
		{
			throw new Exception("Console panel is not visible after running the project");
		}
		Console.WriteLine("✓ Console panel is visible");
	}

	[Then("I wait for the project to complete")]
	public async Task ThenIWaitForTheProjectToComplete()
	{
		await HomePage.WaitForProjectToComplete();
	}

	[Then("I should see {string} in the console output")]
	public async Task ThenIShouldSeeInTheConsoleOutput(string expectedText)
	{
		var output = await HomePage.GetConsoleOutput();
		var found = output.Any(line => line.Contains(expectedText));
		
		if (!found)
		{
			Console.WriteLine($"Console output ({output.Length} lines):");
			foreach (var line in output)
			{
				Console.WriteLine($"  {line}");
			}
			throw new Exception($"Expected text '{expectedText}' not found in console output");
		}
		Console.WriteLine($"✓ Found '{expectedText}' in console output");
	}
}
