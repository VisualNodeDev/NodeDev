using NodeDev.Core;

namespace NodeDev.Tests;

public class SerializationTests
{
	[Theory]
	[MemberData(nameof(GraphExecutorTests.GetBuildOptions), MemberType = typeof(GraphExecutorTests))]
	public void TestBasicSerialization(SerializableBuildOptions options)
	{
		var graph = GraphExecutorTests.CreateSimpleAddGraph<int, int>(out _, out _, out _);
		var project = graph.SelfClass.Project;

		var serialized = project.Serialize();
		var deserializedProject = Project.Deserialize(serialized);

		Assert.Single(deserializedProject.Classes);
		Assert.Equal(2, deserializedProject.Classes.First().Methods.Count);

		var output = GraphExecutorTests.Run<int>(deserializedProject, options, [1, 2]);

		Assert.Equal(3, output);
	}
}