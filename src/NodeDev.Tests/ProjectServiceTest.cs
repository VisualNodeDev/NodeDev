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

	[Fact]
	public async Task SaveProjectUsesConfiguredDirectoryAndCommitsNameAfterSuccess()
	{
		var projectsDirectory = Path.Combine(Path.GetTempPath(), $"nodedev-project-service-{Guid.NewGuid():N}");
		try
		{
			var options = new AppOptionsContainer("");
			options.AppOptions.ProjectsDirectory = projectsDirectory;
			var projectService = new ProjectService(options);

			await projectService.SaveProjectToFileAsync("SavedProject");

			Assert.Equal("SavedProject", projectService.Project.Settings.ProjectName);
			var projectPath = Path.Combine(projectsDirectory, "SavedProject.ndproj");
			Assert.True(File.Exists(projectPath));
			var savedProject = Project.Deserialize(await File.ReadAllTextAsync(projectPath));
			Assert.Equal("SavedProject", savedProject.Settings.ProjectName);
			Assert.Empty(Directory.EnumerateFiles(projectsDirectory, "*.tmp"));
		}
		finally
		{
			if (Directory.Exists(projectsDirectory))
				Directory.Delete(projectsDirectory, recursive: true);
		}
	}

	[Fact]
	public async Task SaveProjectRejectsTraversalWithoutChangingProjectName()
	{
		var options = new AppOptionsContainer("");
		options.AppOptions.ProjectsDirectory = Path.GetTempPath();
		var projectService = new ProjectService(options);
		var originalName = projectService.Project.Settings.ProjectName;

		await Assert.ThrowsAsync<ArgumentException>(() => projectService.SaveProjectToFileAsync("../escaped"));

		Assert.Equal(originalName, projectService.Project.Settings.ProjectName);
	}
}

