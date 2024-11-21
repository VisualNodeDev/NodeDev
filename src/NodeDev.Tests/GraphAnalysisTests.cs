using NodeDev.Core;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes.Flow;

namespace NodeDev.Tests;

public class GraphAnalysisTests
{
	private Project GetTestProject(string name)
	{
		_ = new Blazor.NodeAttributes.NodeDecorationPosition(default); // ugly way to make sure the NodeDev.Blazor assembly is loaded. Don't judge.

		return Project.Deserialize(File.ReadAllText($"TestProjects/GraphAnalysisTests/{name}.json"));
	}

	private Graph GetMain(string name)
	{
		return GetTestProject(name).Classes.First(x => x.Name == "Program").Methods.First(x => x.Name == "Main").Graph;
	}

	private Connection GetEntryExec(Graph graph)
	{
		return graph.Nodes.Values.OfType<EntryNode>().First().Outputs.First(x => x.Type.IsExec);
	}

	[Fact]
	public void GraphGetChunks_StraightLine_ReturnsNoSubChunks()
	{
		var graph = GetMain("StraightLine");

		var chunks = graph.GetChunks(GetEntryExec(graph), false);

		Assert.Equal(3, chunks.Chunks.Count);
		Assert.True(chunks.Chunks.Take(2).All(x => x.SubChunk == null && x.Output != null));

		// Last chunk is the return
		Assert.Null(chunks.Chunks[2].SubChunk);
		Assert.Null(chunks.Chunks[2].Output);

		// The dead end should be the return
		Assert.NotNull(chunks.DeadEndInputs);
		Assert.Single(chunks.DeadEndInputs);
		Assert.Equal("Return", chunks.DeadEndInputs[0]!.Parent.Name);
	}

	[Fact]
	public void GraphGetChunks_StraightLine_NotReturning_Throws()
	{
		var graph = GetMain("StraightLine_NotReturning");

		Assert.Throws<Graph.DeadEndNotAllowed>(() => graph.GetChunks(GetEntryExec(graph), false));
	}

	[Fact]
	public void GraphGetChunks_Branch_Merge_ReturnsSubChunks()
	{
		var graph = GetMain("Branch_Merge");

		var chunks = graph.GetChunks(GetEntryExec(graph), false);

		Assert.Equal(3, chunks.Chunks.Count);
		Assert.NotNull(chunks.Chunks[0].SubChunk);
		Assert.Equal(2, chunks.Chunks[0].SubChunk!.Count);

		// Both side of the branch should be "Console.WriteLine"
		// Get into the first chunk of each branch's subchunk
		Assert.Equal("Console.WriteLine", chunks.Chunks[0].SubChunk!.ElementAt(0).Value.Chunks[0].Output!.Parent.Name);
		Assert.Equal("Console.WriteLine", chunks.Chunks[0].SubChunk!.ElementAt(1).Value.Chunks[0].Output!.Parent.Name);

		// The second chunk should be the merge "Console.ReadLine"
		Assert.Equal("Console.ReadLine", chunks.Chunks[1].Output!.Parent.Name);

		// the dead end should be the Return
		Assert.NotNull(chunks.DeadEndInputs);
		Assert.Equal("Return", chunks.DeadEndInputs[0]!.Parent.Name);
		Assert.Equal("Return", chunks.Chunks[2]!.Input.Parent.Name);
	}

	[Fact]
	public void GraphGetChunks_Branch_NoMerge_Has2Return()
	{
		var graph = GetMain("Branch_NoMerge");

		var chunks = graph.GetChunks(GetEntryExec(graph), false);

		Assert.Single(chunks.Chunks);
		Assert.NotNull(chunks.DeadEndInputs);
		Assert.Equal(2, chunks.DeadEndInputs.Count);
		Assert.Equal("Return", chunks.DeadEndInputs[0]!.Parent.Name);
		Assert.Equal("Return", chunks.DeadEndInputs[1]!.Parent.Name);
	}

	[Fact]
	public void GraphGetChunks_ForEach_Simple_AllowDeadEnd()
	{
		var graph = GetMain("ForEach_Simple");

		var chunks = graph.GetChunks(GetEntryExec(graph), true);

		Assert.Single(chunks.Chunks);

		// Foreach as two exec outputs
		Assert.NotNull(chunks.Chunks[0].SubChunk);
		Assert.Equal(2, chunks.Chunks[0].SubChunk!.Count);

		// First one ends with a dead end on Console.WriteLine, second one with a Return
		Assert.Equal("Console.WriteLine", chunks.Chunks[0].SubChunk!.ElementAt(0).Value.DeadEndInputs![0].Parent.Name);
		Assert.Equal("Return", chunks.Chunks[0].SubChunk!.ElementAt(1).Value.DeadEndInputs![0].Parent.Name);

		// The global dead end should contain both
		Assert.NotNull(chunks.DeadEndInputs);
		Assert.Equal(2, chunks.DeadEndInputs.Count);
		Assert.Equal("Console.WriteLine", chunks.DeadEndInputs[0]!.Parent.Name);
		Assert.Equal("Return", chunks.DeadEndInputs[1]!.Parent.Name);
	}

	[Fact]
	public void GraphGetChunks_ForEach_WithInnerBranch_HasSubChunkInLoopExec()
	{
		var graph = GetMain("ForEach_WithInnerBranch");

		var chunks = graph.GetChunks(GetEntryExec(graph), false);

		// one big chunk with everything in it
		Assert.Single(chunks.Chunks);

		// Foreach as two exec outputs
		Assert.NotNull(chunks.Chunks[0].SubChunk);
		Assert.Equal(2, chunks.Chunks[0].SubChunk!.Count);

		// First one ends with a dead end on Console.WriteLine, second one with a Return
		Assert.Equal("Console.WriteLine", chunks.Chunks[0].SubChunk!.ElementAt(0).Value.DeadEndInputs![0].Parent.Name);
		Assert.Equal("Return", chunks.Chunks[0].SubChunk!.ElementAt(1).Value.DeadEndInputs![0].Parent.Name);

		// Loop has a subchunk with the branch
		Assert.NotNull(chunks.Chunks[0].SubChunk!.ElementAt(0).Value.Chunks[0].SubChunk);
		Assert.Equal(2, chunks.Chunks[0].SubChunk!.ElementAt(0).Value.Chunks[0].SubChunk!.Count);

		// The global dead end should contain the return and both path of the Branch
		Assert.NotNull(chunks.DeadEndInputs);
		Assert.Equal(3, chunks.DeadEndInputs.Count);
		Assert.Equal("Console.WriteLine", chunks.DeadEndInputs[0]!.Parent.Name);
		Assert.Equal("List<int>.Add", chunks.DeadEndInputs[1]!.Parent.Name);
		Assert.Equal("Return", chunks.DeadEndInputs[2]!.Parent.Name);
	}

	[Fact]
	public void GraphGetChunks_Branch_Remerging_ShouldHaveTwoChunk()
	{
		var graph = GetMain("Branch_Remerging");

		var chunks = graph.GetChunks(GetEntryExec(graph), false);

		Assert.Equal(3, chunks.Chunks.Count);
		Assert.NotNull(chunks.DeadEndInputs);
		Assert.Single(chunks.DeadEndInputs);
		Assert.Equal("Return", chunks.DeadEndInputs[0]!.Parent.Name);
		Assert.Equal("Return", chunks.Chunks[2]!.Input.Parent.Name);

		// The first chunk is the branch
		Assert.NotNull(chunks.Chunks[0].SubChunk);
		Assert.Equal(2, chunks.Chunks[0].SubChunk!.Count);
		// First path of the branch has two Console.WriteLine
		Assert.Equal("Console.WriteLine", chunks.Chunks[0].SubChunk!.ElementAt(0).Value.Chunks[0].Output!.Parent.Name);
		Assert.Equal("Console.WriteLine", chunks.Chunks[0].SubChunk!.ElementAt(0).Value.Chunks[1].Output!.Parent.Name);
		// Second path has the List<int>.Add
		Assert.Equal("List<int>.Add", chunks.Chunks[0].SubChunk!.ElementAt(1).Value.Chunks[0].Output!.Parent.Name);
		// The branch remerge with the Console.ReadLine
		Assert.Equal("Console.ReadLine", chunks.Chunks[0].SubChunk!.ElementAt(0).Value.InputMergePoint!.Parent.Name);
		Assert.Equal("Console.ReadLine", chunks.Chunks[0].SubChunk!.ElementAt(1).Value.InputMergePoint!.Parent.Name);

		// Second chunk is the Console.ReadLine with Return as dead end
		Assert.Equal("Console.ReadLine", chunks.Chunks[1].Output!.Parent.Name);
	}

	[Fact]
	public void GraphGetChunks_Branch_InvalidRemerging_Throws()
	{
		var graph = GetMain("Branch_InvalidRemerging");

		Assert.Throws<Graph.BadMergeException>(() => graph.GetChunks(GetEntryExec(graph), false));
	}

	[Fact]
	public void GraphGetChunks_Foreach_LoopMerge_Throws()
	{
		var graph = GetMain("ForEach_LoopMerge");

		Assert.Throws<Graph.BadMergeException>(() => graph.GetChunks(GetEntryExec(graph), false));
	}

	[Fact]
	public void GraphGetChunks_ForEach_WithInnerBranchMerge_DoesntThrow()
	{
		var graph = GetMain("ForEach_WithInnerBranchMerge");

		var chunks = graph.GetChunks(GetEntryExec(graph), false);

		Assert.Single(chunks.Chunks);
		Assert.NotNull(chunks.DeadEndInputs);
		Assert.Equal(2, chunks.DeadEndInputs.Count);
		Assert.Equal("Console.WriteLine", chunks.DeadEndInputs[0]!.Parent.Name);
		Assert.Equal("Return", chunks.DeadEndInputs[1]!.Parent.Name);

		// The loop chunk contains a subchunk for the branch
		Assert.NotNull(chunks.Chunks[0].SubChunk);
		Assert.Equal(2, chunks.Chunks[0].SubChunk!.Count);
		Assert.NotNull(chunks.Chunks[0].SubChunk!.ElementAt(0).Value.DeadEndInputs);
		Assert.Single(chunks.Chunks[0].SubChunk!.ElementAt(0).Value.DeadEndInputs!);
		Assert.Equal("Console.WriteLine", chunks.Chunks[0].SubChunk!.ElementAt(0).Value.DeadEndInputs![0].Parent.Name);
	}

	[Fact]
	public void GraphGetChunks_Branch_Multiple_Remerging_Allowed()
	{
		var graph = GetMain("Branch_Multiple_Remerging");

		var chunks = graph.GetChunks(GetEntryExec(graph), false);

		Assert.Equal(3, chunks.Chunks.Count);
		// After the big first chunk, we have the Console.ReadLine
		Assert.Equal("Console.ReadLine", chunks.Chunks[1].Output!.Parent.Name);

		// It ends with a Return
		Assert.NotNull(chunks.DeadEndInputs);
		Assert.Equal("Return", chunks.DeadEndInputs[0]!.Parent.Name);
		Assert.Equal("Return", chunks.Chunks[2]!.Input.Parent.Name);
	}
}
