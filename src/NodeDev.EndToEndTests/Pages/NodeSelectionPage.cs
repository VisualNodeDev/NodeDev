using Microsoft.Playwright;

namespace NodeDev.EndToEndTests.Pages;

public class NodeSelectionPage
{
    private readonly IPage User;

    public NodeSelectionPage(Hooks.Hooks hooks)
    {
        User = hooks.User;
    }

    private ILocator SearchSelectionDialog => User.Locator("[data-test-id='nodeSelection']");
    private ILocator SearchSelectionResult => SearchSelectionDialog.Locator(".mud-list-item-text");
    private ILocator SearchSelectionTextBox => SearchSelectionDialog.Locator("[data-test-id='search']");

    public async Task SearchByText(string text)
    {
        await SearchSelectionTextBox.FillAsync(text);
    }

    public async Task<ILocator> SearchAndGetResultByText(string text)
    {
        await SearchByText(text);

        var result = SearchSelectionResult.GetByText(text);
        await result.WaitForVisible();

        return result;
    }

    public async Task SearchAndAcceptByText(string text)
    {
        var result = await SearchAndGetResultByText(text);
        await result.ClickAsync();

        await SearchSelectionDialog.WaitForVisible(WaitForSelectorState.Detached);
    }

}