using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Debug;
using NodeDev.Core.Nodes.Flow;
using System.Reactive.Linq;
using Xunit;
using Xunit.Abstractions;

namespace NodeDev.Tests;

public class ConsoleOutputTests
{
	private readonly ITestOutputHelper output;

	public ConsoleOutputTests(ITestOutputHelper output)
	{
		this.output = output;
	}

	[Fact]
	public void Run_ShouldCaptureConsoleOutput_WhenWriteLineNodeIsUsed()
	{
		// Arrange
		var project = Project.CreateNewDefaultProject(out var mainMethod);
		var graph = mainMethod.Graph;

		// Add a WriteLine node between Entry and Return
		var writeLineNode = new WriteLine(graph);
		graph.Manager.AddNode(writeLineNode);

		// Get Entry and Return nodes
		var entryNode = graph.Nodes.Values.OfType<EntryNode>().First();
		var returnNode = graph.Nodes.Values.OfType<ReturnNode>().First();

		// Connect Entry -> WriteLine -> Return
		// Note: By default there is already a connection between Entry and Return, so we don't need to remove it
		
		// Connect Entry.Exec -> WriteLine.Exec
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[0], writeLineNode.Inputs[0]);

		// Set WriteLine value to "Hello from NodeDev!" - this also resolves the generic type to string
		writeLineNode.Inputs[1].UpdateTypeAndTextboxVisibility(project.TypeFactory.Get<string>(), overrideInitialType: true);
		writeLineNode.Inputs[1].UpdateTextboxText("\"Hello from NodeDev!\"");

		// Connect WriteLine.Exec -> Return.Exec
		graph.Manager.AddNewConnectionBetween(writeLineNode.Outputs[0], returnNode.Inputs[0]);

		// Collect console output
		var consoleOutput = new List<string>();
		var executionStarted = false;
		var executionEnded = false;
		
		var outputSubscription = project.ConsoleOutput.Subscribe(text =>
		{
			output.WriteLine($"Console output: {text}");
			consoleOutput.Add(text);
		});
		
		var executionSubscription = project.GraphExecutionChanged.Subscribe(status =>
		{
			output.WriteLine($"Execution status changed: {status}");
			if (status)
				executionStarted = true;
			else
				executionEnded = true;
		});

		try
		{
			// Act
			output.WriteLine("Starting project run...");
			var result = project.Run(BuildOptions.Debug);
			output.WriteLine($"Project run completed with result: {result}");

			// Assert
			Assert.True(executionStarted, "Execution should have started");
			Assert.True(executionEnded, "Execution should have ended");
			Assert.NotEmpty(consoleOutput);
			Assert.Contains(consoleOutput, line => line.Contains("Hello from NodeDev!"));
		}
		finally
		{
			outputSubscription.Dispose();
			executionSubscription.Dispose();
		}
	}
}
