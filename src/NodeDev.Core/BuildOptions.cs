using NodeDev.Core.Nodes;

namespace NodeDev.Core;

public record class BuildOptions(BuildExpressionOptions BuildExpressionOptions, bool PreBuildOnly)
{
    public static readonly BuildOptions Debug = new(BuildExpressionOptions.Debug, false);

    public static readonly BuildOptions Release = new(BuildExpressionOptions.Release, false);
}
