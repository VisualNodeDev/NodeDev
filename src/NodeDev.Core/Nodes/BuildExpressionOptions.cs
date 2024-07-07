namespace NodeDev.Core.Nodes;

public record class BuildExpressionOptions(bool RaiseNodeExecutedEvents)
{
    public static readonly BuildExpressionOptions Debug = new(true);

    public static readonly BuildExpressionOptions Release = new(false);
}
