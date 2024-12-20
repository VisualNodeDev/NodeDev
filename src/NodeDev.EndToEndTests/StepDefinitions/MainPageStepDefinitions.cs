using Microsoft.Playwright;
using NodeDev.EndToEndTests.Pages;

namespace NodeDev.EndToEndTests.StepDefinitions;

[Binding]
public sealed class MainPageStepDefinitions
{
	private readonly IPage User;
	private readonly HomePage HomePage;

	public MainPageStepDefinitions(Hooks.Hooks hooks, HomePage homePage)
	{
		User = hooks.User;
		HomePage = homePage;
	}

	[Given("I load the default project")]
	public async Task GivenILoadTheDefaultProject()
	{
		await HomePage.CreateNewProject();
	}

	[Then("The {string} method in the {string} class should exist")]
	public async Task ThenTheMethodInTheClassShouldExist(string method, string className)
	{
		await HomePage.OpenProjectExplorerProjectTab();

		await HomePage.HasClass(className);

		await HomePage.ClickClass(className);

		await HomePage.OpenProjectExplorerClassTab();

		await HomePage.HasMethodByName(method);
	}

	[Given("I save the current project")]
	public async Task GivenISaveTheCurrentProject()
	{
		await HomePage.SaveProject();
	}

	[Then("Snackbar should contain {string}")]
	public async Task ThenSnackbarShouldContain(string text)
	{
		await HomePage.SnackBarHasByText(text);
	}

	[Given("I set the projects directory to {string}")]
	public async Task GivenISetTheProjectsDirectoryTo(string directory)
	{
		await HomePage.OpenOptionsDialog();
		await HomePage.SetProjectsDirectory(directory);
		await HomePage.AcceptOptions();
	}

	[Given("I save the current project as {string}")]
	public async Task GivenISaveTheCurrentProjectAs(string projectName)
	{
		await HomePage.OpenSaveAsDialog();
		await HomePage.SetProjectNameAs(projectName);
		await HomePage.AcceptSaveAs();
	}

}
