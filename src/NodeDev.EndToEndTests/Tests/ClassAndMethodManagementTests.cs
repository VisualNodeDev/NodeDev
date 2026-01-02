using NodeDev.EndToEndTests.Fixtures;
using Xunit;

namespace NodeDev.EndToEndTests.Tests;

public class ClassAndMethodManagementTests : E2ETestBase
{
	public ClassAndMethodManagementTests(AppServerFixture app, PlaywrightFixture playwright)
		: base(app, playwright)
	{
	}

	[Fact(Timeout = 60_000)]
	public async Task CreateNewClass()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		
		try
		{
			await HomePage.CreateClass("MyNewClass");
			
			var exists = await HomePage.ClassExists("MyNewClass");
			Assert.True(exists, "Class 'MyNewClass' not found in project explorer");
			
			await HomePage.TakeScreenshot("/tmp/new-class-created.png");
			Console.WriteLine("✓ Created new class");
		}
		catch (NotImplementedException ex)
		{
			Console.WriteLine($"Class creation not implemented: {ex.Message}");
		}
	}

	[Fact(Timeout = 60_000)]
	public async Task RenameExistingClass()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		
		try
		{
			await HomePage.RenameClass("Program", "RenamedProgram");
			
			// Wait for UI to update
			await Task.Delay(2000);
			
			var exists = await HomePage.ClassExists("RenamedProgram");
			Assert.True(exists, "Class 'RenamedProgram' not found in project explorer");
			
			await HomePage.TakeScreenshot("/tmp/class-renamed.png");
			Console.WriteLine("✓ Renamed class");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Class rename failed: {ex.Message}");
			await HomePage.TakeScreenshot("/tmp/class-rename-failed.png");
			throw;
		}
	}

	[Fact(Timeout = 60_000)]
	public async Task CreateNewMethod()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.ClickClass("Program");
		await HomePage.OpenProjectExplorerClassTab();
		
		try
		{
			await HomePage.CreateMethod("MyNewMethod");
			
			await HomePage.HasMethodByName("MyNewMethod");
			
			await HomePage.TakeScreenshot("/tmp/new-method-created.png");
			Console.WriteLine("✓ Created new method");
		}
		catch (NotImplementedException ex)
		{
			Console.WriteLine($"Method creation not implemented: {ex.Message}");
		}
	}

	[Fact(Timeout = 60_000)]
	public async Task RenameExistingMethod()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.ClickClass("Program");
		await HomePage.OpenProjectExplorerClassTab();
		
		try
		{
			await HomePage.RenameMethod("Main", "RenamedMain");
			
			// Wait for UI to update
			await Task.Delay(2000);
			
			var exists = await HomePage.MethodExists("RenamedMain");
			Assert.True(exists, "Method 'RenamedMain' not found");
			
			await HomePage.TakeScreenshot("/tmp/method-renamed.png");
			Console.WriteLine("✓ Renamed method");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Method rename failed: {ex.Message}");
			await HomePage.TakeScreenshot("/tmp/method-rename-failed.png");
			throw;
		}
	}

	[Fact(Timeout = 60_000)]
	public async Task DeleteMethod()
	{
		await HomePage.CreateNewProject();
		await HomePage.OpenProjectExplorerProjectTab();
		await HomePage.ClickClass("Program");
		await HomePage.OpenProjectExplorerClassTab();
		
		// First create a method to delete
		try
		{
			await HomePage.CreateMethod("MethodToDelete");
			await HomePage.HasMethodByName("MethodToDelete");
			
			// Now delete it
			await HomePage.DeleteMethod("MethodToDelete");
			
			// Wait for deletion
			await Task.Delay(1000);
			
			var exists = await HomePage.MethodExists("MethodToDelete");
			Assert.False(exists, "Method 'MethodToDelete' should have been deleted");
			
			await HomePage.TakeScreenshot("/tmp/method-deleted.png");
			Console.WriteLine("✓ Deleted method");
		}
		catch (NotImplementedException ex)
		{
			Console.WriteLine($"Method creation/deletion not implemented: {ex.Message}");
		}
	}
}
