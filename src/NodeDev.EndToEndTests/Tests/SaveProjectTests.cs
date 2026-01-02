using NodeDev.EndToEndTests.Fixtures;
using Xunit;

namespace NodeDev.EndToEndTests.Tests;

public class SaveProjectTests : E2ETestBase
{
	public SaveProjectTests(AppServerFixture app, PlaywrightFixture playwright)
		: base(app, playwright)
	{
	}

	[Fact(Timeout = 60_000)]
	public async Task SaveEmptyProject()
	{
		// Load default project
		await HomePage.CreateNewProject();
		
		// Verify Main method exists
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenProjectExplorerClassTab();
		await HomePage.HasMethodByName("Main");
		
		// Save the project
		await HomePage.OpenSaveAsDialog();
		await HomePage.SetProjectNameAs("EmptyProject");
		await HomePage.AcceptSaveAs();
		
		// Verify save was successful
		await HomePage.SnackBarHasByText("Project saved");
		Console.WriteLine("✓ Project saved successfully");
	}
}
