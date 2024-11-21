using NodeDev.Core.Nodes;

namespace NodeDev.Core;

public record class BuildOptions(BuildExpressionOptions BuildExpressionOptions, bool PreBuildOnly, string OutputPath)
{
	public static readonly BuildOptions Debug = new(BuildExpressionOptions.Debug, false, "bin/Debug");

	public static readonly BuildOptions Release = new(BuildExpressionOptions.Release, false, "bin/Release");
}
