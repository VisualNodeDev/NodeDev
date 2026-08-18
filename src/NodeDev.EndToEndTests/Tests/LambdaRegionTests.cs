using Microsoft.Playwright;
using NodeDev.EndToEndTests.Fixtures;
using Xunit;

namespace NodeDev.EndToEndTests.Tests;

public class LambdaRegionTests : E2ETestBase
{
	public LambdaRegionTests(AppServerFixture app, PlaywrightFixture playwright)
		: base(app, playwright)
	{
	}

	[Fact(Timeout = 60_000)]
	public async Task CreateFunc_ShowsLiveRegionBodyAndSignatureControls()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.HasClass("Program");
		await HomePage.ClickClass("Program");
		await HomePage.OpenMethod("Main");
		SetupConsoleMonitoring();

		await HomePage.SearchForNodes("CreateFuncNode");
		await HomePage.AddNodeFromSearch("CreateFuncNode");

		var region = Page.Locator("[data-test-id='lambda-region']").Last;
		await region.WaitForAsync(new() { State = WaitForSelectorState.Visible });
		await region.Locator("[data-test-id='graph-node'][data-test-node-name='Lambda Entry']").WaitForAsync(new() { State = WaitForSelectorState.Visible });
		await region.Locator("[data-test-id='graph-node'][data-test-node-name='Lambda Return']").WaitForAsync(new() { State = WaitForSelectorState.Visible });
		await region.Locator("[data-test-id='lambda-delegate-port']").WaitForAsync(new() { State = WaitForSelectorState.Visible });

		await region.Locator("[data-test-id='lambda-add-parameter']").ClickAsync();
		var rebuiltRegion = Page.Locator("[data-test-id='lambda-region']").Last;
		await rebuiltRegion.Locator("[data-test-id='lambda-parameter']").WaitForAsync(new() { State = WaitForSelectorState.Visible });

		await HomePage.SearchForNodes("TypeOf");
		await HomePage.AddNodeFromSearch("TypeOf");
		var sourcePort = Page
			.Locator("[data-test-id='graph-node'][data-test-node-name='TypeOf']")
			.Last
			.Locator(".col.output")
			.Filter(new() { HasText = "Type" })
			.Locator(".diagram-port")
			.First;
		var resultPort = rebuiltRegion
			.Locator("[data-test-id='graph-node'][data-test-node-name='Lambda Return']")
			.Locator(".col.input")
			.Filter(new() { HasText = "Result" })
			.Locator(".diagram-port")
			.First;
		await DragPortToPort(sourcePort, resultPort);

		rebuiltRegion = Page.Locator("[data-test-id='lambda-region']").Last;
		await rebuiltRegion.Locator("[data-test-id='lambda-capture']").WaitForAsync(new() { State = WaitForSelectorState.Visible });
		await rebuiltRegion.Locator("[data-test-id='lambda-capture-port']").WaitForAsync(new() { State = WaitForSelectorState.Visible });

		AssertNoConsoleErrors();
	}

	private async Task DragPortToPort(ILocator source, ILocator destination)
	{
		await source.WaitForAsync(new() { State = WaitForSelectorState.Visible });
		await destination.WaitForAsync(new() { State = WaitForSelectorState.Visible });
		var sourceBox = await source.BoundingBoxAsync() ?? throw new InvalidOperationException("Source port has no bounds.");
		var destinationBox = await destination.BoundingBoxAsync() ?? throw new InvalidOperationException("Destination port has no bounds.");

		await Page.Mouse.MoveAsync((float)(sourceBox.X + sourceBox.Width / 2), (float)(sourceBox.Y + sourceBox.Height / 2));
		await Page.Mouse.DownAsync();
		await Page.Mouse.MoveAsync((float)(destinationBox.X + destinationBox.Width / 2), (float)(destinationBox.Y + destinationBox.Height / 2), new() { Steps = 20 });
		await Page.Mouse.UpAsync();
	}
}
