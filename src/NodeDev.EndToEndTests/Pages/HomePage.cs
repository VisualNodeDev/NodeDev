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
}