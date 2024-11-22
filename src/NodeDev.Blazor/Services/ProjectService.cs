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

		public void SaveProjectToFile()
		{
			ArgumentNullException.ThrowIfNullOrWhiteSpace(Project.Settings.ProjectName);
			var projectPath = Path.Combine(AppOptionsContainer.AppOptions.ProjectsDirectory!, $"{Project.Settings.ProjectName}.ndproj");
			string content = Project.Serialize();
			File.WriteAllText(projectPath, content);

		}
	}
}
