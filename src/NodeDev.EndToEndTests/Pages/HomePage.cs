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
        var saveBtn = SearchAppBar.Locator("[data-test-id='Save']");

        await saveBtn.WaitForVisible();

        await saveBtn.ClickAsync();
    }

    public async Task SnackBarHasByText(string text)
    {
        await SearchSnackBarContainer.GetByText(text).WaitForVisible();
    }
}