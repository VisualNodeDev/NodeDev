using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NodeDev.Blazor.Services;

namespace NodeDev.Tests;

public class WorkspaceCommandServiceTests
{
	[Fact]
	/// <summary>
	/// Verifies that a successful create command returns the same class instance added to the active project.
	/// </summary>
	public void CreateClassReturnsEntityAndUpdatesProject()
	{
		var commands = CreateCommands();

		var result = commands.CreateClass("Customer");

		Assert.True(result.Succeeded);
		Assert.NotNull(result.Value);
		Assert.Contains(result.Value, commands.Project.Classes);
	}

	[Fact]
	/// <summary>
	/// Verifies that duplicate classes are reported as command failures without adding a second class.
	/// </summary>
	public void DuplicateClassIsReturnedAsFailure()
	{
		var commands = CreateCommands();
		Assert.True(commands.CreateClass("Customer").Succeeded);

		var result = commands.CreateClass("Customer");

		Assert.False(result.Succeeded);
		Assert.IsType<InvalidOperationException>(result.Exception);
		Assert.Single(commands.Project.Classes, nodeClass => nodeClass.Name == "Customer");
	}

	[Fact]
	/// <summary>
	/// Verifies that command-side validation prevents blank method names from mutating the model.
	/// </summary>
	public void InvalidMethodNameDoesNotMutateClass()
	{
		var commands = CreateCommands();
		var nodeClass = commands.Project.Classes.First();
		var methodCount = nodeClass.Methods.Count;

		var result = commands.CreateMethod(nodeClass, "  ");

		Assert.False(result.Succeeded);
		Assert.Equal(methodCount, nodeClass.Methods.Count);
	}

	[Fact]
	/// <summary>
	/// Verifies that the project and command services share a circuit-scoped lifetime.
	/// </summary>
	public void WorkspaceServicesAreScoped()
	{
		var services = new ServiceCollection();
		services.AddNodeDev();

		Assert.Equal(ServiceLifetime.Scoped, services.Single(descriptor => descriptor.ServiceType == typeof(ProjectService)).Lifetime);
		Assert.Equal(ServiceLifetime.Scoped, services.Single(descriptor => descriptor.ServiceType == typeof(WorkspaceCommandService)).Lifetime);
	}

	/// <summary>
	/// Creates a command service backed by an isolated default project for each test.
	/// </summary>
	/// <returns>A workspace command service with a no-op logger.</returns>
	private static WorkspaceCommandService CreateCommands()
	{
		var options = new AppOptionsContainer("");
		var projectService = new ProjectService(options);
		return new WorkspaceCommandService(projectService, NullLogger<WorkspaceCommandService>.Instance);
	}
}
