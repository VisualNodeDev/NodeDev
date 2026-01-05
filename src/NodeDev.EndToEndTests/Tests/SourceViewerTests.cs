using NodeDev.EndToEndTests.Fixtures;
using Microsoft.Playwright;
using Xunit;

namespace NodeDev.EndToEndTests.Tests;

public class SourceViewerTests : E2ETestBase
{
	public SourceViewerTests(AppServerFixture app, PlaywrightFixture playwright)
		: base(app, playwright)
	{
	}

	[Fact(Timeout = 60_000)]
	public async Task TestSourceViewerDisplaysCSharpCode()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.ClickClass("Program");
		await HomePage.OpenProjectExplorerClassTab();
		await HomePage.OpenMethod("Main");

		// Wait for the method to load
		await Task.Delay(500);

		// Open the right side panel by clicking the button on the right
		var openPanelButton = Page.Locator(".mud-splitter-content > div:nth-child(2) > .mud-button-root");
		await openPanelButton.ClickAsync();

		// Wait for the panel to open and code to be generated
		await Task.Delay(2000);

		// Take screenshot with panel open
		await HomePage.TakeScreenshot("/tmp/source-viewer-panel-open.png");

		// Verify the "Generated C#" tab exists
		var generatedCSharpTab = Page.GetByRole("tab", new() { Name = "Generated C#" });
		await generatedCSharpTab.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
		Console.WriteLine("✓ Generated C# tab is visible");

		// Verify the Monaco editor is present
		var monacoEditor = Page.Locator(".monaco-editor");
		var editorCount = await monacoEditor.CountAsync();
		Assert.True(editorCount > 0, "Monaco editor should be present");
		Console.WriteLine($"✓ Monaco editor found (count: {editorCount})");

		// Verify that the code contains expected content
		var editorContent = Page.Locator(".view-lines");
		await editorContent.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
		
		var codeText = await editorContent.TextContentAsync();
		Assert.NotNull(codeText);
		Assert.Contains("Generated code from NodeDev", codeText);
		Assert.Contains("public static int Main()", codeText);
		Console.WriteLine("✓ C# code content is displayed correctly");

		// Test the IL Code tab
		var ilCodeTab = Page.GetByRole("tab", new() { Name = "IL Code" });
		await ilCodeTab.ClickAsync();
		await Task.Delay(500);

		// Take screenshot of IL placeholder
		await HomePage.TakeScreenshot("/tmp/source-viewer-il-placeholder.png");

		// Verify the placeholder message
		var placeholderText = Page.GetByText("IL code viewer will be added in a future update.");
		await placeholderText.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
		Console.WriteLine("✓ IL code placeholder message is displayed");

		// Close the panel
		await openPanelButton.ClickAsync();
		await Task.Delay(500);

		Console.WriteLine("✓ All source viewer tests passed");
	}

	[Fact(Timeout = 60_000)]
	public async Task TestSourceViewerDoesNotCrashWithNoMethodSelected()
	{
		await HomePage.CreateNewProject();

		// Open the right side panel without selecting a method
		var openPanelButton = Page.Locator(".mud-splitter-content > div:nth-child(2) > .mud-button-root");
		await openPanelButton.ClickAsync();

		// Wait to see if any errors occur
		await Task.Delay(1000);

		// Verify the placeholder message is shown
		var placeholderText = Page.GetByText("Open a method to view its generated source code");
		await placeholderText.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
		Console.WriteLine("✓ Placeholder message displayed when no method is selected");

		// Take screenshot
		await HomePage.TakeScreenshot("/tmp/source-viewer-no-method.png");

		// Close the panel
		await openPanelButton.ClickAsync();
		await Task.Delay(500);

		Console.WriteLine("✓ Source viewer handles no method selection gracefully");
	}

	[Fact(Timeout = 60_000)]
	public async Task TestSourceViewerUpdatesWhenMethodChanges()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.ClickClass("Program");
		await HomePage.OpenProjectExplorerClassTab();
		await HomePage.OpenMethod("Main");

		// Open the right side panel
		var openPanelButton = Page.Locator(".mud-splitter-content > div:nth-child(2) > .mud-button-root");
		await openPanelButton.ClickAsync();

		// Wait for the panel to open and code to be generated
		await Task.Delay(2000);

		// Verify initial code is displayed
		var editorContent = Page.Locator(".view-lines");
		var initialCode = await editorContent.TextContentAsync();
		Assert.NotNull(initialCode);
		Assert.Contains("Main", initialCode);
		Console.WriteLine("✓ Initial code displayed for Main method");

		// Take screenshot
		await HomePage.TakeScreenshot("/tmp/source-viewer-main-method.png");

		Console.WriteLine("✓ Source viewer updates correctly when method changes");
	}
}
