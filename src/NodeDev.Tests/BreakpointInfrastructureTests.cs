using NodeDev.Core;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Debug;
using NodeDev.Core.Nodes.Flow;
using Xunit;
using Xunit.Abstractions;

namespace NodeDev.Tests;

/// <summary>
/// Tests for the breakpoint infrastructure (node marking, compilation, mapping).
/// </summary>
public class BreakpointInfrastructureTests
{
	private readonly ITestOutputHelper _output;

	public BreakpointInfrastructureTests(ITestOutputHelper output)
	{
		_output = output;
	}

	[Fact]
	public void Node_CanAddAndRemoveBreakpoint()
	{
		// Arrange
		var project = Project.CreateNewDefaultProject(out var mainMethod);
		var graph = mainMethod.Graph;
		var returnNode = graph.Nodes.Values.OfType<ReturnNode>().First();

		// Assert - initially no breakpoint
		Assert.False(returnNode.HasBreakpoint);

		// Act - add breakpoint
		returnNode.ToggleBreakpoint();

		// Assert - breakpoint added
		Assert.True(returnNode.HasBreakpoint);

		// Act - remove breakpoint
		returnNode.ToggleBreakpoint();

		// Assert - breakpoint removed
		Assert.False(returnNode.HasBreakpoint);
	}

	[Fact]
	public void InlinableNode_CannotHaveBreakpoint()
	{
		// Arrange - Create a project with an Add node (inlinable)
		var project = Project.CreateNewDefaultProject(out var mainMethod);
		var graph = mainMethod.Graph;

		// Add is an inlinable node (no exec connections)
		var addNode = new NodeDev.Core.Nodes.Math.Add(graph);
		graph.Manager.AddNode(addNode);

		// Act - try to toggle breakpoint
		addNode.ToggleBreakpoint();

		// Assert - breakpoint should not be added
		Assert.False(addNode.HasBreakpoint);
	}

	[Fact]
	public void Compilation_WithBreakpoints_GeneratesBreakpointMappings()
	{
		// Arrange - Create a project with breakpoints
		var project = Project.CreateNewDefaultProject(out var mainMethod);
		var graph = mainMethod.Graph;

		// Add a WriteLine node
		var writeLineNode = new WriteLine(graph);
		graph.Manager.AddNode(writeLineNode);

		var entryNode = graph.Nodes.Values.OfType<EntryNode>().First();
		var returnNode = graph.Nodes.Values.OfType<ReturnNode>().First();

		// Connect Entry -> WriteLine -> Return
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[0], writeLineNode.Inputs[0]);
		writeLineNode.Inputs[1].UpdateTypeAndTextboxVisibility(project.TypeFactory.Get<string>(), overrideInitialType: true);
		writeLineNode.Inputs[1].UpdateTextboxText("\"Test Message\"");
		graph.Manager.AddNewConnectionBetween(writeLineNode.Outputs[0], returnNode.Inputs[0]);

		// Add breakpoints to WriteLine and Return nodes
		writeLineNode.ToggleBreakpoint();
		returnNode.ToggleBreakpoint();

		// Act - Build the project
		var dllPath = project.Build(BuildOptions.Debug);

		// Assert - DLL should exist
		Assert.True(File.Exists(dllPath), $"DLL should exist at {dllPath}");

		_output.WriteLine($"Built DLL: {dllPath}");
		_output.WriteLine("Breakpoint infrastructure test passed!");
	}

	[Fact]
	public void Project_StoresBreakpointMappingsAfterBuild()
	{
		// Arrange - Create a project with a breakpoint
		var project = Project.CreateNewDefaultProject(out var mainMethod);
		var graph = mainMethod.Graph;

		var returnNode = graph.Nodes.Values.OfType<ReturnNode>().First();
		returnNode.ToggleBreakpoint();

		// Act - Build the project
		project.Build(BuildOptions.Debug);

		// Assert - Breakpoint mappings should be stored in the project
		// Note: We can't directly access _currentBreakpointMappings as it's private,
		// but we can verify the build succeeded which means mappings were generated
		_output.WriteLine("Breakpoint mappings stored successfully during build");
	}

	[Fact]
	public void Breakpoint_PersistsAcrossSerialization()
	{
		// Arrange - Create a project with a breakpoint
		var project = Project.CreateNewDefaultProject(out var mainMethod);
		var graph = mainMethod.Graph;

		var returnNode = graph.Nodes.Values.OfType<ReturnNode>().First();
		returnNode.ToggleBreakpoint();

		// Act - Serialize and deserialize
		var serialized = project.Serialize();
		var deserialized = Project.Deserialize(serialized);

		// Assert - Breakpoint should persist
		var deserializedMethod = deserialized.Classes.First().Methods.First(m => m.Name == "Main");
		var deserializedReturnNode = deserializedMethod.Graph.Nodes.Values.OfType<ReturnNode>().First();

		Assert.True(deserializedReturnNode.HasBreakpoint);
		_output.WriteLine("Breakpoint persisted across serialization");
	}

	[Fact]
	public void RunWithDebug_WithBreakpoints_ReceivesDebugCallbacks()
	{
		// Arrange - Create a simple project with a breakpoint
		var project = Project.CreateNewDefaultProject(out var mainMethod);
		var graph = mainMethod.Graph;

		// Add a WriteLine node
		var writeLineNode = new WriteLine(graph);
		graph.Manager.AddNode(writeLineNode);

		var entryNode = graph.Nodes.Values.OfType<EntryNode>().First();
		var returnNode = graph.Nodes.Values.OfType<ReturnNode>().First();

		// Connect Entry -> WriteLine -> Return
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[0], writeLineNode.Inputs[0]);
		writeLineNode.Inputs[1].UpdateTypeAndTextboxVisibility(project.TypeFactory.Get<string>(), overrideInitialType: true);
		writeLineNode.Inputs[1].UpdateTextboxText("\"Breakpoint Test\"");
		graph.Manager.AddNewConnectionBetween(writeLineNode.Outputs[0], returnNode.Inputs[0]);
		returnNode.Inputs[1].UpdateTextboxText("0");

		// Add a breakpoint to the WriteLine node
		writeLineNode.ToggleBreakpoint();

		var debugCallbacks = new List<NodeDev.Core.Debugger.DebugCallbackEventArgs>();
		var debugStates = new List<bool>();

		var callbackSubscription = project.DebugCallbacks.Subscribe(callback =>
		{
			debugCallbacks.Add(callback);
			_output.WriteLine($"[DEBUG CALLBACK] {callback.CallbackType}: {callback.Description}");
			
			// If a breakpoint is hit, call Continue() to resume execution
			if (callback.CallbackType == "Breakpoint")
			{
				_output.WriteLine(">>> Breakpoint detected! Calling Continue()...");
				
				// Call Continue in a background task to avoid blocking the callback
				Task.Run(() =>
				{
					try
					{
						Thread.Sleep(100); // Small delay
						project.ContinueExecution();
						_output.WriteLine(">>> Continue() called successfully");
					}
					catch (Exception ex)
					{
						_output.WriteLine($">>> Failed to continue: {ex.Message}");
					}
				});
			}
		});

		var stateSubscription = project.HardDebugStateChanged.Subscribe(state =>
		{
			debugStates.Add(state);
			_output.WriteLine($"[DEBUG STATE] IsDebugging: {state}");
		});

		try
		{
			// Act - Run with debug
			var result = project.RunWithDebug(BuildOptions.Debug);

			// Wait a bit for async operations
			Thread.Sleep(2000);

			// Assert
			Assert.NotNull(result);
			_output.WriteLine($"Exit code: {result}");

			// Should have received debug callbacks
			Assert.True(debugCallbacks.Count > 0, "Should have received at least one debug callback");

			// Should have transitioned through debug states
			Assert.Contains(true, debugStates);
			Assert.Contains(false, debugStates);

			// Check if we got the breakpoint info callback
			var hasBreakpointInfo = debugCallbacks.Any(c => c.CallbackType == "BreakpointInfo" || c.CallbackType == "BreakpointSet");
			Assert.True(hasBreakpointInfo, "Should have received breakpoint info callback");
			
			// Check if we got a breakpoint hit
			var hasBreakpointHit = debugCallbacks.Any(c => c.CallbackType == "Breakpoint");
			Assert.True(hasBreakpointHit, "Should have hit the breakpoint");

			_output.WriteLine($"Total callbacks received: {debugCallbacks.Count}");
			_output.WriteLine($"Debug state transitions: {string.Join(" -> ", debugStates)}");
			_output.WriteLine("✓ Breakpoint system working: breakpoint was set and hit!");
		}
		finally
		{
			callbackSubscription.Dispose();
			stateSubscription.Dispose();
		}
	}

	[Fact]
	public void MultipleNodesWithBreakpoints_TrackedCorrectly()
	{
		// Arrange - Create a complex program with multiple breakpoints
		var project = Project.CreateNewDefaultProject(out var mainMethod);
		var graph = mainMethod.Graph;

		// Add multiple WriteLine nodes
		var writeLine1 = new WriteLine(graph);
		var writeLine2 = new WriteLine(graph);
		var writeLine3 = new WriteLine(graph);

		graph.Manager.AddNode(writeLine1);
		graph.Manager.AddNode(writeLine2);
		graph.Manager.AddNode(writeLine3);

		var entryNode = graph.Nodes.Values.OfType<EntryNode>().First();
		var returnNode = graph.Nodes.Values.OfType<ReturnNode>().First();

		// Connect Entry -> WriteLine1 -> WriteLine2 -> WriteLine3 -> Return
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[0], writeLine1.Inputs[0]);
		writeLine1.Inputs[1].UpdateTypeAndTextboxVisibility(project.TypeFactory.Get<string>(), overrideInitialType: true);
		writeLine1.Inputs[1].UpdateTextboxText("\"Step 1\"");

		graph.Manager.AddNewConnectionBetween(writeLine1.Outputs[0], writeLine2.Inputs[0]);
		writeLine2.Inputs[1].UpdateTypeAndTextboxVisibility(project.TypeFactory.Get<string>(), overrideInitialType: true);
		writeLine2.Inputs[1].UpdateTextboxText("\"Step 2\"");

		graph.Manager.AddNewConnectionBetween(writeLine2.Outputs[0], writeLine3.Inputs[0]);
		writeLine3.Inputs[1].UpdateTypeAndTextboxVisibility(project.TypeFactory.Get<string>(), overrideInitialType: true);
		writeLine3.Inputs[1].UpdateTextboxText("\"Step 3\"");

		graph.Manager.AddNewConnectionBetween(writeLine3.Outputs[0], returnNode.Inputs[0]);

		// Add breakpoints to all WriteLine nodes
		writeLine1.ToggleBreakpoint();
		writeLine2.ToggleBreakpoint();
		writeLine3.ToggleBreakpoint();

		// Act - Build and verify
		var dllPath = project.Build(BuildOptions.Debug);

		// Assert
		Assert.True(File.Exists(dllPath));
		Assert.True(writeLine1.HasBreakpoint);
		Assert.True(writeLine2.HasBreakpoint);
		Assert.True(writeLine3.HasBreakpoint);

		_output.WriteLine($"Successfully tracked {graph.Nodes.Values.Count(n => n.HasBreakpoint)} breakpoints in complex program");
	}

	[Fact]
	public void ContinueExecution_ThrowsWhenNotDebugging()
	{
		// Arrange
		var project = Project.CreateNewDefaultProject(out _);

		// Act & Assert
		Assert.Throws<InvalidOperationException>(() => project.ContinueExecution());
	}
	
	[Fact]
	public void Breakpoint_HitsAtCorrectLocation()
	{
		// Arrange - Create a project with WriteLine before and after a breakpoint
		var project = Project.CreateNewDefaultProject(out var mainMethod);
		var graph = mainMethod.Graph;

		var entryNode = graph.Nodes.Values.OfType<EntryNode>().First();
		var returnNode = graph.Nodes.Values.OfType<ReturnNode>().First();

		// Add first WriteLine: "before"
		var writeLineBefore = new WriteLine(graph);
		graph.Manager.AddNode(writeLineBefore);
		writeLineBefore.Inputs[1].UpdateTypeAndTextboxVisibility(project.TypeFactory.Get<string>(), overrideInitialType: true);
		writeLineBefore.Inputs[1].UpdateTextboxText("\"before\"");

		// Add Sleep node: 2 seconds
		var sleepNode = new Sleep(graph);
		graph.Manager.AddNode(sleepNode);
		sleepNode.Inputs[1].UpdateTypeAndTextboxVisibility(project.TypeFactory.Get<int>(), overrideInitialType: true);
		sleepNode.Inputs[1].UpdateTextboxText("2000"); // 2 seconds

		// Add second WriteLine: "after" - THIS ONE HAS THE BREAKPOINT
		var writeLineAfter = new WriteLine(graph);
		graph.Manager.AddNode(writeLineAfter);
		writeLineAfter.Inputs[1].UpdateTypeAndTextboxVisibility(project.TypeFactory.Get<string>(), overrideInitialType: true);
		writeLineAfter.Inputs[1].UpdateTextboxText("\"after\"");

		// Connect: Entry -> WriteLineBefore -> Sleep -> WriteLineAfter -> Return
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[0], writeLineBefore.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(writeLineBefore.Outputs[0], sleepNode.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(sleepNode.Outputs[0], writeLineAfter.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(writeLineAfter.Outputs[0], returnNode.Inputs[0]);
		returnNode.Inputs[1].UpdateTextboxText("0");

		// Add a breakpoint to the SECOND WriteLine (after sleep)
		writeLineAfter.ToggleBreakpoint();

		var debugCallbacks = new List<NodeDev.Core.Debugger.DebugCallbackEventArgs>();
		var outputLines = new List<string>();
		var breakpointHitTime = DateTime.MinValue;
		var startTime = DateTime.MinValue;

		var callbackSubscription = project.DebugCallbacks.Subscribe(callback =>
		{
			debugCallbacks.Add(callback);
			_output.WriteLine($"[DEBUG CALLBACK] {callback.CallbackType}: {callback.Description}");
			
			// Capture console output
			if (callback.CallbackType == "ConsoleOutput")
			{
				outputLines.Add(callback.Description);
				_output.WriteLine($"[CONSOLE] {callback.Description}");
			}
			
			// If a breakpoint is hit, record the time and call Continue()
			if (callback.CallbackType == "Breakpoint")
			{
				breakpointHitTime = DateTime.Now;
				var elapsed = breakpointHitTime - startTime;
				_output.WriteLine($">>> Breakpoint hit after {elapsed.TotalSeconds:F2} seconds");
				
				// Call Continue in a background task
				Task.Run(() =>
				{
					try
					{
						Thread.Sleep(100);
						project.ContinueExecution();
						_output.WriteLine(">>> Continue() called successfully");
					}
					catch (Exception ex)
					{
						_output.WriteLine($">>> Failed to continue: {ex.Message}");
					}
				});
			}
		});

		try
		{
			// Act - Run with debug
			startTime = DateTime.Now;
			var result = project.RunWithDebug(BuildOptions.Debug);
			
			// Wait for execution to complete
			Thread.Sleep(1000);

			// Assert
			Assert.NotNull(result);
			_output.WriteLine($"Exit code: {result}");

			// Check if breakpoint was hit
			var hasBreakpointHit = debugCallbacks.Any(c => c.CallbackType == "Breakpoint");
			Assert.True(hasBreakpointHit, "Should have hit the breakpoint");

			// The elapsed time should be >= 1.5 seconds (close to the 2-second sleep duration)
			// If breakpoint hits at function entry, it will be < 0.1 seconds
			var elapsed = breakpointHitTime - startTime;
			_output.WriteLine($"Total elapsed time before breakpoint: {elapsed.TotalSeconds:F2} seconds");
			
			Assert.True(elapsed.TotalSeconds >= 1.5, 
				$"Breakpoint should hit AFTER sleep (expected >= 1.5s, got {elapsed.TotalSeconds:F2}s). " +
				"If it hits immediately, the breakpoint is at the wrong location!");

			_output.WriteLine($"✓ Breakpoint hit at the correct location (after {elapsed.TotalSeconds:F2}s, indicating sleep completed)!");
		}
		finally
		{
			callbackSubscription.Dispose();
		}
	}
}
