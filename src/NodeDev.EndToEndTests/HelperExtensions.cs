using Microsoft.Playwright;

namespace NodeDev.EndToEndTests;

internal static class HelperExtensions
{
	public static Task WaitForVisible(this ILocator locator, WaitForSelectorState state = WaitForSelectorState.Visible)
	{
		return locator.WaitForAsync(new() { State = state });
	}
}
