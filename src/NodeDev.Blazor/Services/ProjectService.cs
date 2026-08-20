using NodeDev.Core;

namespace NodeDev.Blazor.Services
{
	/// <summary>
	/// Service used to keep a singleton of the project throughout the application.
	/// </summary>
	public class ProjectService
	{
		public Project Project { get; private set; }

		private readonly AppOptionsContainer AppOptionsContainer;

		public delegate void ProjectChangedHandler();
		/// <summary>
		/// Event used to notify subscribers when the current project has changed.
		/// </summary>
		public event ProjectChangedHandler? ProjectChanged;

		/// <summary>
		/// Instanciates a default project as the current project.
		/// </summary>
		public ProjectService(AppOptionsContainer appOptionsContainer)
		{
			Project = Project.CreateNewDefaultProject();
			AppOptionsContainer = appOptionsContainer;
		}

		/// <summary>
		/// Changes the current project and notifies all subscribers of <see cref="ProjectChanged" />.
		/// </summary>
		/// <param name="project"></param>
		public void ChangeProject(Project project)
		{
			Project = project;
			ProjectChanged?.Invoke();
		}

		public async Task LoadProjectFromFileAsync(string file)
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(file);

			var json = await File.ReadAllTextAsync(file);
			var project = Project.Deserialize(json);
			project.Settings.ProjectName = Path.GetFileNameWithoutExtension(file);
			ChangeProject(project);
		}

		public Task LoadProjectAsync(string projectName)
		{
			return LoadProjectFromFileAsync(GetProjectFilePath(projectName));
		}

		public IReadOnlyList<string> GetSavedProjectNames()
		{
			var projectsDirectory = GetProjectsDirectory();
			if (!Directory.Exists(projectsDirectory))
				return [];

			return Directory.EnumerateFiles(projectsDirectory, "*.ndproj")
				.Select(Path.GetFileNameWithoutExtension)
				.Where(name => !string.IsNullOrWhiteSpace(name))
				.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
				.ToList()!;
		}

		public async Task SaveProjectToFileAsync(string? projectName = null, CancellationToken cancellationToken = default)
		{
			projectName ??= Project.Settings.ProjectName;
			var projectPath = GetProjectFilePath(projectName);
			var projectsDirectory = Path.GetDirectoryName(projectPath)!;
			Directory.CreateDirectory(projectsDirectory);

			var temporaryPath = Path.Combine(projectsDirectory, $".{Guid.NewGuid():N}.tmp");
			try
			{
				var serializedProject = Project.Serialize(Project.Settings with { ProjectName = projectName });
				await File.WriteAllTextAsync(temporaryPath, serializedProject, cancellationToken);
				File.Move(temporaryPath, projectPath, overwrite: true);
				Project.Settings.ProjectName = projectName;
			}
			finally
			{
				if (File.Exists(temporaryPath))
					File.Delete(temporaryPath);
			}
		}

		private string GetProjectFilePath(string projectName)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(projectName);
			if (projectName is "." or ".." || projectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || projectName.Contains('/') || projectName.Contains('\\'))
				throw new ArgumentException("Project name contains invalid filename characters.", nameof(projectName));

			var projectsDirectory = GetProjectsDirectory();
			var projectPath = Path.GetFullPath(Path.Combine(projectsDirectory, $"{projectName}.ndproj"));
			var relativePath = Path.GetRelativePath(projectsDirectory, projectPath);
			if (Path.IsPathRooted(relativePath) || relativePath == ".." || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
				throw new InvalidOperationException("The project path must remain inside the configured projects directory.");

			return projectPath;
		}

		private string GetProjectsDirectory()
		{
			var configuredDirectory = AppOptionsContainer.AppOptions.ProjectsDirectory;
			if (string.IsNullOrWhiteSpace(configuredDirectory))
				throw new InvalidOperationException("Configure a projects directory in Options before opening or saving projects.");

			return Path.GetFullPath(configuredDirectory);
		}
	}
}
