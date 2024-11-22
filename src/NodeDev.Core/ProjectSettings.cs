namespace NodeDev.Core;

public record class ProjectSettings()
{
	public string ProjectName { get; set; } = string.Empty;
	public static ProjectSettings Default { get; } = new();
}
