namespace NodeDev.Core;

public class ProjectSettings()
{
    public string ProjectName { get; set; } = string.Empty;
    public static ProjectSettings Default { get; } = new();

}
