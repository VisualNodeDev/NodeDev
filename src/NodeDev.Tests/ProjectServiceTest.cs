using NodeDev.Blazor.Services;
using NodeDev.Core;

namespace NodeDev.Tests;

public class ProjectServiceTest
{
	[Fact]
	public void TestsChangeProject()
	{
		var project = new Project(Guid.NewGuid());
		var projectService = new ProjectService(new AppOptionsContainer(""));

		projectService.ChangeProject(project);

		Assert.Equal(project, projectService.Project);
	}

	[Fact]
	public void TestsProjectChangedEvent()
	{
		var project = new Project(Guid.NewGuid());
		var projectService = new ProjectService(new AppOptionsContainer(""));
		bool isEventTriggered = false;

		projectService.ProjectChanged += () => isEventTriggered = true;
		projectService.ChangeProject(project);

		Assert.True(isEventTriggered);
	}
}

