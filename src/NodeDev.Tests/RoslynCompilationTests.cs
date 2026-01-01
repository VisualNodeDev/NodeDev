using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Nodes.Math;
using Xunit;

namespace NodeDev.Tests;

public class RoslynCompilationTests
{
	[Fact]
	public void SimpleAddMethodCompilation()
	{
		// Create a simple project with an Add method
		var project = new Project(Guid.NewGuid());
		var myClass = new NodeClass("TestClass", "MyProject", project);
		project.AddClass(myClass);

		// Create a method that adds two integers: int Add(int a, int b) { return a + b; }
		var method = new NodeClassMethod(myClass, "Add", project.TypeFactory.Get<int>());
		method.IsStatic = true;
		myClass.AddMethod(method, createEntryAndReturn: false);
		
		// Add parameters
		method.Parameters.Add(new("a", project.TypeFactory.Get<int>(), method));
		method.Parameters.Add(new("b", project.TypeFactory.Get<int>(), method));

		var graph = method.Graph;
		
		// Create nodes
		var entryNode = new EntryNode(graph);
		var addNode = new Add(graph);
		var returnNode = new ReturnNode(graph);

		// Add nodes to graph
		graph.Manager.AddNode(entryNode);
		graph.Manager.AddNode(addNode);
		graph.Manager.AddNode(returnNode);

		// Connect nodes
		// entry.Exec -> return.Exec
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[0], returnNode.Inputs[0]);
		
		// entry.a -> add.a
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[1], addNode.Inputs[0]);
		// entry.b -> add.b
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[2], addNode.Inputs[1]);
		
		// add.c -> return.Return
		graph.Manager.AddNewConnectionBetween(addNode.Outputs[0], returnNode.Inputs[1]);

		// Compile with Roslyn
		var buildOptions = BuildOptions.Debug;
		
		var compiler = new RoslynNodeClassCompiler(project, buildOptions);
		RoslynNodeClassCompiler.CompilationResult result;
		try
		{
			result = compiler.Compile();
		}
		catch (RoslynNodeClassCompiler.CompilationException ex)
		{
			// Print source code for debugging
			Console.WriteLine("=== Generated Source Code (Compilation Failed) ===");
			Console.WriteLine(ex.SourceCode);
			Console.WriteLine("=== End of Source Code ===");
			throw;
		}
		
		// Print generated source code for debugging
		Console.WriteLine("=== Generated Source Code ===");
		Console.WriteLine(result.SourceCode);
		Console.WriteLine("=== End of Source Code ===");

		var assembly = result.Assembly;

		// Verify the assembly was created
		Assert.NotNull(assembly);
		
		// Get the type and method
		var type = assembly.GetType("MyProject.TestClass");
		Assert.NotNull(type);
		
		var addMethod = type.GetMethod("Add");
		Assert.NotNull(addMethod);
		
		// Invoke the method
		var invokeResult = addMethod.Invoke(null, new object[] { 5, 3 });
		Assert.Equal(8, invokeResult);
	}

	[Fact]
	public void SimpleBranchMethodCompilation()
	{
		// Create a method with a branch: int Max(int a, int b) { if (a > b) return a; else return b; }
		var project = new Project(Guid.NewGuid());
		var myClass = new NodeClass("TestClass", "MyProject", project);
		project.AddClass(myClass);

		var method = new NodeClassMethod(myClass, "Max", project.TypeFactory.Get<int>());
		method.IsStatic = true;
		myClass.AddMethod(method, createEntryAndReturn: false);
		
		method.Parameters.Add(new("a", project.TypeFactory.Get<int>(), method));
		method.Parameters.Add(new("b", project.TypeFactory.Get<int>(), method));

		var graph = method.Graph;
		
		// Create nodes
		var entryNode = new EntryNode(graph);
		var biggerThanNode = new BiggerThan(graph);
		var branchNode = new Branch(graph);
		var returnNodeTrue = new ReturnNode(graph);
		var returnNodeFalse = new ReturnNode(graph);

		graph.Manager.AddNode(entryNode);
		graph.Manager.AddNode(biggerThanNode);
		graph.Manager.AddNode(branchNode);
		graph.Manager.AddNode(returnNodeTrue);
		graph.Manager.AddNode(returnNodeFalse);

		// Connect: entry -> branch
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[0], branchNode.Inputs[0]);
		
		// Connect: entry.a -> biggerThan.a, entry.b -> biggerThan.b
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[1], biggerThanNode.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[2], biggerThanNode.Inputs[1]);
		
		// Connect: biggerThan.c -> branch.Condition
		graph.Manager.AddNewConnectionBetween(biggerThanNode.Outputs[0], branchNode.Inputs[1]);
		
		// Connect: branch.IfTrue -> returnNodeTrue, branch.IfFalse -> returnNodeFalse
		graph.Manager.AddNewConnectionBetween(branchNode.Outputs[0], returnNodeTrue.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(branchNode.Outputs[1], returnNodeFalse.Inputs[0]);
		
		// Connect: entry.a -> returnNodeTrue.Return, entry.b -> returnNodeFalse.Return
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[1], returnNodeTrue.Inputs[1]);
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[2], returnNodeFalse.Inputs[1]);

		// Compile with Roslyn
		var buildOptions = BuildOptions.Debug;
		var assembly = project.BuildWithRoslyn(buildOptions);

		// Verify
		Assert.NotNull(assembly);
		var type = assembly.GetType("MyProject.TestClass");
		Assert.NotNull(type);
		var maxMethod = type.GetMethod("Max");
		Assert.NotNull(maxMethod);
		
		// Test the method
		Assert.Equal(10, maxMethod.Invoke(null, new object[] { 10, 5 }));
		Assert.Equal(10, maxMethod.Invoke(null, new object[] { 5, 10 }));
		Assert.Equal(7, maxMethod.Invoke(null, new object[] { 7, 7 }));
	}
}
