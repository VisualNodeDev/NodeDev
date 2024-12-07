using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Debug;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;
using System.Diagnostics;

namespace NodeDev.Tests;

public class GraphExecutorTests
{
	public static Graph CreateSimpleAddGraph<TIn, TOut>(out EntryNode entryNode, out ReturnNode returnNode, out Core.Nodes.Math.Add addNode)
	{
		var project = new Project(Guid.NewGuid());
		var nodeClass = new NodeClass("Program", "Test", project);
		project.AddClass(nodeClass);

		var graph = new Graph();
		var method = new NodeClassMethod(nodeClass, "MainInternal", nodeClass.TypeFactory.Get<TOut>(), graph, true);
		nodeClass.AddMethod(method, createEntryAndReturn: false);
		graph.SelfMethod = method;

		method.Parameters.Add(new("A", nodeClass.TypeFactory.Get<TIn>(), method));
		method.Parameters.Add(new("B", nodeClass.TypeFactory.Get<TIn>(), method));

		entryNode = new EntryNode(graph);

		returnNode = new ReturnNode(graph);

		addNode = new Core.Nodes.Math.Add(graph);

		addNode.Inputs[0].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<TIn>(), overrideInitialType: true);
		addNode.Inputs[1].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<TIn>(), overrideInitialType: true);
		addNode.Outputs[0].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<TOut>(), overrideInitialType: true);

		graph.Manager.AddNode(entryNode);
		graph.Manager.AddNode(addNode);
		graph.Manager.AddNode(returnNode);

		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[0], returnNode.Inputs[0]);

		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[1], addNode.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[2], addNode.Inputs[1]);
		graph.Manager.AddNewConnectionBetween(addNode.Outputs[0], returnNode.Inputs[1]);

		CreateStaticMainWithConversion(nodeClass, method);

		return graph;
	}

	public static Graph CreateStaticMainWithConversion(NodeClass nodeClass, NodeClassMethod internalMethod)
	{
		// Now that the fake method is created we need to create the real Main method, taking string[] as input and converting the first two elements to TIn
		var graph = new Graph();
		var mainMethod = new NodeClassMethod(nodeClass, "Main", nodeClass.TypeFactory.Void, graph, true);
		nodeClass.AddMethod(mainMethod, createEntryAndReturn: false);
		graph.SelfMethod = mainMethod;

		mainMethod.Parameters.Add(new("args", nodeClass.TypeFactory.Get<string[]>(), mainMethod));

		var entryNode = new EntryNode(graph);

		var returnNode = new ReturnNode(graph);

		var internalMethodCall = new MethodCall(graph);
		internalMethodCall.SetMethodTarget(internalMethod);

		var convertMethodInfo = new RealMethodInfo(nodeClass.TypeFactory, typeof(Convert).GetMethod("ChangeType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, [typeof(object), typeof(Type)])!, nodeClass.TypeFactory.Get(typeof(Convert), null));

		Node lastNode = entryNode;

		graph.Manager.AddNode(entryNode);
		graph.Manager.AddNode(internalMethodCall);
		graph.Manager.AddNode(returnNode);

		int index = 0;
		foreach (var parameter in internalMethod.Parameters)
		{
			// Create a method call to "Convert.ChangeType"
			var parseNode = new MethodCall(graph);
			parseNode.SetMethodTarget(convertMethodInfo);
			graph.Manager.AddNode(parseNode);

			// Get the value of the current parameter from the "args" array
			var arrayGet = new ArrayGet(graph);
			arrayGet.Inputs[0].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<string[]>(), overrideInitialType: true);
			arrayGet.Inputs[1].UpdateTextboxText(index++.ToString());
			arrayGet.Outputs[0].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<string>(), overrideInitialType: true);
			graph.Manager.AddNode(arrayGet);

			// Connect the array to the array get node
			graph.Manager.AddNewConnectionBetween(entryNode.Outputs[1], arrayGet.Inputs[0]); // Since we already incremented the index, this skips the "Exec" output of the entry node

			// Connect the array get to the parse method call
			graph.Manager.AddNewConnectionBetween(arrayGet.Outputs[0], parseNode.Inputs[1]);

			// Connect the output type we want into the parse node
			// For that, we need a "TypeOf" node
			var typeOfNode = new TypeOf(graph);
			typeOfNode.OnBeforeGenericTypeDefined(new Dictionary<string, TypeBase>()
			{
				["T"] = parameter.ParameterType
			});
			graph.Manager.AddNode(typeOfNode);

			// Connect the typeof to the convert method
			graph.Manager.AddNewConnectionBetween(typeOfNode.Outputs[0], parseNode.Inputs[2]);

			// Connect the last node to the convert method
			graph.Manager.AddNewConnectionBetween(lastNode.Outputs[0], parseNode.Inputs[0]);

			// Before we can plug the converted value into the internal method call, we need to cast the "object" returned by Convert.ChangeType to the actual type
			var castNode = new Cast(graph);
			castNode.Inputs[0].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<object>(), overrideInitialType: true);
			castNode.Outputs[0].UpdateTypeAndTextboxVisibility(parameter.ParameterType, overrideInitialType: true);
			graph.Manager.AddNode(castNode);

			// Connect the convert method to the cast node
			graph.Manager.AddNewConnectionBetween(parseNode.Outputs[1], castNode.Inputs[0]);

			// Connect the casted value to the return's node input
			graph.Manager.AddNewConnectionBetween(castNode.Outputs[0], internalMethodCall.Inputs[index]);// Since we already incremented the index, this skips the "Exec" input of the node

			// The last node is now the cast node, since it's the last node to contain an exec
			lastNode = parseNode;
		}

		// Connect the last node to the internal method call
		graph.Manager.AddNewConnectionBetween(lastNode.Outputs[0], internalMethodCall.Inputs[0]);

		// Add a "Console.WriteLine" to print the returned value
		var writeLine = new WriteLine(graph);
		writeLine.Inputs[1].UpdateTypeAndTextboxVisibility(internalMethodCall.Outputs[1].Type, overrideInitialType: true);
		graph.Manager.AddNode(writeLine);

		// Connect the internal method call to the return node
		graph.Manager.AddNewConnectionBetween(internalMethodCall.Outputs[0], writeLine.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(writeLine.Outputs[0], returnNode.Inputs[0]);

		// Connect the output of the internal method call to the return node
		graph.Manager.AddNewConnectionBetween(internalMethodCall.Outputs[1], writeLine.Inputs[1]);


		return graph;
	}

	public static T Run<T>(Project project, BuildOptions buildOptions, params object[]? parameters)
	{
		parameters ??= [];

		try
		{
			var path = project.Build(buildOptions);

			var arguments = Path.GetFileName(path) + " " + string.Join(" ", parameters.Select(x => '"' + (x?.ToString() ?? "") + '"'));
			var process = new Process()
			{
				StartInfo = new ProcessStartInfo()
				{
					FileName = "dotnet",
					Arguments = arguments,
					WorkingDirectory = Path.GetDirectoryName(path),
					RedirectStandardOutput = true,
				}
			};

			process.Start();

			process.WaitForExit();

			string? line = null;
			while (true)
			{
				var newLine = process.StandardOutput.ReadLine();
				if (newLine != null)
					line = newLine;
				else
					break;
			}

			if (line == null)
				throw new Exception("No output from the process");

			return (T)Convert.ChangeType(line, typeof(T));
		}
		finally
		{
			// Clean up
			if (Directory.Exists(buildOptions.OutputPath))
				Directory.Delete(buildOptions.OutputPath, true);
		}
	}

	public static TheoryData<SerializableBuildOptions> GetBuildOptions() => new([new(true), new(false)]);

	[Theory]
	[MemberData(nameof(GetBuildOptions))]
	public void SimpleAdd(SerializableBuildOptions options)
	{
		var graph = CreateSimpleAddGraph<int, int>(out _, out _, out _);

		var output = Run<int>(graph.Project, options, [1, 2]);

		Assert.Equal(3, output);
	}

	[Theory]
	[MemberData(nameof(GetBuildOptions))]
	public void SimpleAdd_CheckTypeFloat(SerializableBuildOptions options)
	{
		var graph = CreateSimpleAddGraph<float, float>(out _, out _, out _);

		var output = Run<float>(graph.Project, options, [1.5f, 2f]);

		Assert.Equal(3.5f, output);
	}

	[Theory]
	[MemberData(nameof(GetBuildOptions))]
	public void TestBranch(SerializableBuildOptions options)
	{
		var graph = CreateSimpleAddGraph<int, int>(out var entryNode, out var returnNode1, out var addNode);
		graph.Manager.DisconnectConnectionBetween(entryNode.Outputs[0], returnNode1.Inputs[0]);
		graph.Manager.DisconnectConnectionBetween(returnNode1.Inputs[1], addNode.Outputs[0]);
		returnNode1.Inputs[1].UpdateTextboxText("1");

		var smallerThan = new Core.Nodes.Math.SmallerThan(graph);
		smallerThan.Inputs[0].UpdateTypeAndTextboxVisibility(graph.SelfClass.TypeFactory.Get<int>(), overrideInitialType: true);
		smallerThan.Inputs[1].UpdateTypeAndTextboxVisibility(graph.SelfClass.TypeFactory.Get<int>(), overrideInitialType: true);
		smallerThan.Inputs[1].UpdateTextboxText("0");
		graph.Manager.AddNode(smallerThan);
		graph.Manager.AddNewConnectionBetween(addNode.Outputs[0], smallerThan.Inputs[0]);

		var returnNode2 = new Core.Nodes.Flow.ReturnNode(graph);
		returnNode2.Inputs[1].UpdateTextboxText("0");
		graph.Manager.AddNode(returnNode2);

		var branchNode = new Core.Nodes.Flow.Branch(graph);
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[0], branchNode.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(smallerThan.Outputs[0], branchNode.Inputs[1]);
		graph.Manager.AddNode(branchNode);

		graph.Manager.AddNewConnectionBetween(branchNode.Outputs[0], returnNode1.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(branchNode.Outputs[1], returnNode2.Inputs[0]);

		var output = Run<int>(graph.Project, options, [1, 2]);
		Assert.Equal(0, output);

		output = Run<int>(graph.Project, options, [1, -2]);
		Assert.Equal(1, output);
	}

	[Theory]
	[MemberData(nameof(GetBuildOptions))]
	public void TestProjectRun(SerializableBuildOptions options)
	{
		var graph = CreateSimpleAddGraph<int, int>(out var entryNode, out var returnNode1, out var addNode);
		graph.Manager.DisconnectConnectionBetween(entryNode.Outputs[0], returnNode1.Inputs[0]);
		graph.Manager.DisconnectConnectionBetween(returnNode1.Inputs[1], addNode.Outputs[0]);
		returnNode1.Inputs[1].UpdateTextboxText("1");

		var smallerThan = new Core.Nodes.Math.SmallerThan(graph);
		graph.Manager.AddNode(smallerThan);
		smallerThan.Inputs[0].UpdateTypeAndTextboxVisibility(graph.SelfClass.TypeFactory.Get<int>(), overrideInitialType: true);
		smallerThan.Inputs[1].UpdateTypeAndTextboxVisibility(graph.SelfClass.TypeFactory.Get<int>(), overrideInitialType: true);
		smallerThan.Inputs[1].UpdateTextboxText("0");
		graph.Manager.AddNewConnectionBetween(addNode.Outputs[0], smallerThan.Inputs[0]);

		var returnNode2 = new Core.Nodes.Flow.ReturnNode(graph);
		graph.Manager.AddNode(returnNode2);
		returnNode2.Inputs[1].UpdateTextboxText("0");

		var branchNode = new Core.Nodes.Flow.Branch(graph);
		graph.Manager.AddNode(branchNode);
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[0], branchNode.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(smallerThan.Outputs[0], branchNode.Inputs[1]);

		graph.Manager.AddNewConnectionBetween(branchNode.Outputs[0], returnNode1.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(branchNode.Outputs[1], returnNode2.Inputs[0]);

		var output = Run<int>(graph.Project, options, [1, 2]);

		Assert.Equal(0, output);

		output = Run<int>(graph.Project, options, [-1, -2]);
		Assert.Equal(1, output);
	}

	// This test validates the TryCatchNode by simulating a scenario where an exception is thrown and caught.
	// The test sets up a graph with an entry node, a TryCatchNode, and return nodes for both try and catch blocks.
	// The try block attempts to parse an invalid integer string, which throws an exception.
	// The catch block returns 1, indicating that the exception was caught.
	// The test asserts that the output is 1, confirming that the exception was caught and handled correctly.
	[Theory]
	[MemberData(nameof(GetBuildOptions))]
	public void TestTryCatchNode(SerializableBuildOptions options)
	{
		var project = new Project(Guid.NewGuid());
		var nodeClass = new NodeClass("Program", "Test", project);
		project.AddClass(nodeClass);

		var graph = new Graph();
		var method = new NodeClassMethod(nodeClass, "MainInternal", nodeClass.TypeFactory.Get<int>(), graph);
		method.IsStatic = true;
		nodeClass.AddMethod(method, createEntryAndReturn: false);
		graph.SelfMethod = nodeClass.Methods.First();

		var entryNode = new EntryNode(graph);
		var tryCatchNode = new TryCatchNode(graph);
		tryCatchNode.Outputs[3].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<Exception>(), overrideInitialType: true);

		graph.Manager.AddNode(entryNode);
		graph.Manager.AddNode(tryCatchNode);

		// Create local variable
		var declareVariableNode = new DeclareVariableNode(graph);
		declareVariableNode.Inputs[1].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<int>(), true); // Initial value
		declareVariableNode.Outputs[1].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<int>(), true); // Variable
		graph.Manager.AddNode(declareVariableNode);

		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[0], declareVariableNode.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(declareVariableNode.Outputs[0], tryCatchNode.Inputs[0]);

		var returnNode = new ReturnNode(graph);
		graph.Manager.AddNewConnectionBetween(declareVariableNode.Outputs[1], returnNode.Inputs[1]);
		graph.Manager.AddNode(returnNode);

		// Create the catch block body
		var catchVariableNode = new SetVariableValueNode(graph);
		catchVariableNode.Inputs[1].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<int>(), true); // Variable
		catchVariableNode.Inputs[2].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<int>(), true); // Value
		catchVariableNode.Inputs[2].UpdateTextboxText("2");
		graph.Manager.AddNode(catchVariableNode);
		graph.Manager.AddNewConnectionBetween(tryCatchNode.Outputs[1], catchVariableNode.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(declareVariableNode.Outputs[1], catchVariableNode.Inputs[1]);
		graph.Manager.AddNewConnectionBetween(catchVariableNode.Outputs[0], returnNode.Inputs[0]);

		// Create the try block body
		var parseNode = new MethodCall(graph);
		parseNode.SetMethodTarget(new RealMethodInfo(nodeClass.TypeFactory, typeof(int).GetMethod("Parse", new[] { typeof(string) })!, nodeClass.TypeFactory.Get<int>()));
		parseNode.Inputs[1].UpdateTextboxText("invalid");
		graph.Manager.AddNode(parseNode);
		graph.Manager.AddNewConnectionBetween(tryCatchNode.Outputs[0], parseNode.Inputs[0]);

		var tryVariableNode = new SetVariableValueNode(graph);
		tryVariableNode.Inputs[1].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<int>(), true); // Variable
		tryVariableNode.Inputs[2].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<int>(), true); // Value
		tryVariableNode.Inputs[2].UpdateTextboxText("1");
		graph.Manager.AddNode(tryVariableNode);

		graph.Manager.AddNewConnectionBetween(parseNode.Outputs[0], tryVariableNode.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(declareVariableNode.Outputs[1], tryVariableNode.Inputs[1]);
		graph.Manager.AddNewConnectionBetween(tryVariableNode.Outputs[0], returnNode.Inputs[0]);

		CreateStaticMainWithConversion(nodeClass, method);

		var output = Run<int>(project, options);

		Assert.Equal(2, output);
	}

	[Theory]
	[MemberData(nameof(GetBuildOptions))]
	public void TestDeclareAndSetVariable(SerializableBuildOptions options)
	{
		var project = new Project(Guid.NewGuid());
		var nodeClass = new NodeClass("Program", "Test", project);
		project.AddClass(nodeClass);

		var graph = new Graph();
		var method = new NodeClassMethod(nodeClass, "MainInternal", nodeClass.TypeFactory.Get<int>(), graph, true);
		nodeClass.AddMethod(method, createEntryAndReturn: false);
		graph.SelfMethod = method;

		method.Parameters.Add(new("A", nodeClass.TypeFactory.Get<int>(), method));

		var entryNode = new EntryNode(graph);
		graph.Manager.AddNode(entryNode);

		var declareVariableNode = new DeclareVariableNode(graph);
		declareVariableNode.Inputs[1].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<int>(), true); // Initial value
		declareVariableNode.Outputs[1].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<int>(), true); // Variable
		graph.Manager.AddNode(declareVariableNode);

		var setVariableValueNode = new SetVariableValueNode(graph);
		setVariableValueNode.Inputs[1].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<int>(), true); // Variable
		setVariableValueNode.Inputs[2].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<int>(), true); // Value
		graph.Manager.AddNode(setVariableValueNode);

		var returnNode = new ReturnNode(graph);
		graph.Manager.AddNode(returnNode);

		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[0], declareVariableNode.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(declareVariableNode.Outputs[0], setVariableValueNode.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(declareVariableNode.Outputs[1], setVariableValueNode.Inputs[1]);
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[1], setVariableValueNode.Inputs[2]);
		graph.Manager.AddNewConnectionBetween(setVariableValueNode.Outputs[0], returnNode.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(declareVariableNode.Outputs[1], returnNode.Inputs[1]);

		CreateStaticMainWithConversion(nodeClass, method);

		var output = Run<int>(graph.Project, options, [5]);

		Assert.Equal(5, output);
	}

	[Theory]
	[MemberData(nameof(GetBuildOptions))]
	public void TestDeclareVariableDefaultValue(SerializableBuildOptions options)
	{
		var project = new Project(Guid.NewGuid());
		var nodeClass = new NodeClass("Program", "Test", project);
		project.AddClass(nodeClass);

		var graph = new Graph();
		var method = new NodeClassMethod(nodeClass, "MainInternal", nodeClass.TypeFactory.Get<int>(), graph, true);
		nodeClass.AddMethod(method, createEntryAndReturn: false);
		graph.SelfMethod = method;

		method.Parameters.Add(new("A", nodeClass.TypeFactory.Get<int>(), method));

		var entryNode = new EntryNode(graph);
		graph.Manager.AddNode(entryNode);

		var declareVariableNode = new DeclareVariableNode(graph);
		declareVariableNode.Inputs[1].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<int>(), true);
		declareVariableNode.Outputs[1].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<int>(), true);
		graph.Manager.AddNode(declareVariableNode);

		var returnNode = new ReturnNode(graph);
		graph.Manager.AddNode(returnNode);

		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[0], declareVariableNode.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(entryNode.Outputs[1], declareVariableNode.Inputs[1]);
		graph.Manager.AddNewConnectionBetween(declareVariableNode.Outputs[0], returnNode.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(declareVariableNode.Outputs[1], returnNode.Inputs[1]);

		CreateStaticMainWithConversion(nodeClass, method);

		var output = Run<int>(graph.Project, options, [5]);

		Assert.Equal(5, output);
	}
}
