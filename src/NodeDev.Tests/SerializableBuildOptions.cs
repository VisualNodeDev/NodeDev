using NodeDev.Core;
using NodeDev.Core.Nodes;
using Xunit.Abstractions;

namespace NodeDev.Tests;

public class SerializableBuildOptions : IXunitSerializable
{
    public bool Debug;
    public SerializableBuildOptions(bool debug)
    {
        Debug = debug;
    }

    public SerializableBuildOptions()
    { }

    public void Deserialize(IXunitSerializationInfo info)
    {
        Debug = info.GetValue<bool>("Debug");
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        info.AddValue("Debug", Debug);
    }

    // implicit conversion between SerializableBuildOptions and BuildOptions
    public static implicit operator BuildOptions(SerializableBuildOptions options) => 
		new (options.Debug ? BuildExpressionOptions.Debug : BuildExpressionOptions.Release, false, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
}
