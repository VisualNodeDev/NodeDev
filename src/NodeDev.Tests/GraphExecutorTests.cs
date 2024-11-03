using MudBlazor;
using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Debug;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;
using System.Diagnostics;

namespace NodeDev.Tests;

public class GraphExecutorTests
{
	public static Graph CreateSimpleAddGraph<TIn, TOut>(out Core.Nodes.Flow.EntryNode entryNode, out Core.Nodes.Flow.ReturnNode returnNode, out Core.Nodes.Math.Add addNode)
	{
		var project = new Project(Guid.NewGuid());
		var nodeClass = new NodeClass("Program", "Test", project);
		project.Classes.Add(nodeClass);

		var graph = new Graph();
		var method = new NodeClassMethod(nodeClass, "MainInternal", nodeClass.TypeFactory.Get<TOut>(), graph, true);
		nodeClass.Methods.Add(method);
		graph.SelfMethod = method;

		method.Parameters.Add(new("A", nodeClass.TypeFactory.Get<TIn>(), method));
		method.Parameters.Add(new("B", nodeClass.TypeFactory.Get<TIn>(), method));

		entryNode = new EntryNode(graph);

		returnNode = new ReturnNode(graph);
		returnNode.Inputs.Add(new("Result", entryNode, nodeClass.TypeFactory.Get<TOut>()));

		addNode = new Core.Nodes.Math.Add(graph);

		addNode.Inputs[0].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<TIn>(), overrideInitialType: true);
		addNode.Inputs[1].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<TIn>(), overrideInitialType: true);
		addNode.Outputs[0].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<TOut>(), overrideInitialType: true);

		graph.AddNode(entryNode, false);
		graph.AddNode(addNode, false);
		graph.AddNode(returnNode, false);

		graph.Connect(entryNode.Outputs[0], returnNode.Inputs[0], false);

		graph.Connect(entryNode.Outputs[1], addNode.Inputs[0], false);
		graph.Connect(entryNode.Outputs[2], addNode.Inputs[1], false);
		graph.Connect(addNode.Outputs[0], returnNode.Inputs[1], false);

		CreateStaticMainWithConversion(nodeClass, method);

		return graph;
	}

	public static Graph CreateStaticMainWithConversion(NodeClass nodeClass, NodeClassMethod internalMethod)
	{
		// Now that the fake method is created we need to create the real Main method, taking string[] as input and converting the first two elements to TIn
		var graph = new Graph();
		var mainMethod = new NodeClassMethod(nodeClass, "Main", nodeClass.TypeFactory.Get<int>(), graph, true);
		nodeClass.Methods.Add(mainMethod);
		graph.SelfMethod = mainMethod;

		mainMethod.Parameters.Add(new("args", nodeClass.TypeFactory.Get<string[]>(), mainMethod));

		var entryNode = new EntryNode(graph);

		var returnNode = new ReturnNode(graph);
		returnNode.Inputs.Add(new("Result", entryNode, nodeClass.TypeFactory.Get<int>()));

		var internalMethodCall = new MethodCall(graph);
		internalMethodCall.SetMethodTarget(internalMethod);

		var convertMethodInfo = new RealMethodInfo(nodeClass.TypeFactory, typeof(Convert).GetMethod("ChangeType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, [typeof(object), typeof(Type)])!, nodeClass.TypeFactory.Get(typeof(Convert), null));

		Node lastNode = entryNode;

		graph.AddNode(entryNode, false);
		graph.AddNode(internalMethodCall, false);
		graph.AddNode(returnNode, false);

		int index = 0;
		foreach (var parameter in internalMethod.Parameters)
		{
			// Create a method call to "Convert.ChangeType"
			var parseNode = new MethodCall(graph);
			parseNode.SetMethodTarget(convertMethodInfo);
			graph.AddNode(parseNode, false);

			// Get the value of the current parameter from the "args" array
			var arrayGet = new ArrayGet(graph);
			arrayGet.Inputs[0].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<string[]>(), overrideInitialType: true);
			arrayGet.Inputs[1].UpdateTextboxText(index++.ToString());
			arrayGet.Outputs[0].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<string>(), overrideInitialType: true);
			graph.AddNode(arrayGet, false);

			// Connect the array to the array get node
			graph.Connect(entryNode.Outputs[index], arrayGet.Inputs[0], false); // Since we already incremented the index, this skips the "Exec" output of the entry node

			// Connect the array get to the parse method call
			graph.Connect(arrayGet.Outputs[0], parseNode.Inputs[1], false);

			// Connect the output type we want into the parse node
			// For that, we need a "TypeOf" node
			var typeOfNode = new TypeOf(graph);
			typeOfNode.OnBeforeGenericTypeDefined(new Dictionary<string, TypeBase>()
			{
				["T"] = parameter.ParameterType
			});
			graph.AddNode(typeOfNode, false);

			// Connect the typeof to the convert method
			graph.Connect(typeOfNode.Outputs[0], parseNode.Inputs[2], false);

			// Connect the last node to the convert method
			graph.Connect(lastNode.Outputs[0], parseNode.Inputs[0], false);

			// Before we can plug the converted value into the internal method call, we need to cast the "object" returned by Convert.ChangeType to the actual type
			var castNode = new Cast(graph);
			castNode.Inputs[0].UpdateTypeAndTextboxVisibility(nodeClass.TypeFactory.Get<object>(), overrideInitialType: true);
			castNode.Outputs[0].UpdateTypeAndTextboxVisibility(parameter.ParameterType, overrideInitialType: true);
			graph.AddNode(castNode, false);

			// Connect the convert method to the cast node
			graph.Connect(parseNode.Outputs[1], castNode.Inputs[0], false);

			// Connect the casted value to the return's node input
			graph.Connect(castNode.Outputs[0], internalMethodCall.Inputs[index], false);// Since we already incremented the index, this skips the "Exec" input of the node

			// The last node is now the cast node, since it's the last node to contain an exec
			lastNode = parseNode;
		}

		// Connect the last node to the internal method call
		graph.Connect(lastNode.Outputs[0], internalMethodCall.Inputs[0], false);

		// Add a "Console.WriteLine" to print the returned value
		var writeLine = new WriteLine(graph);
		writeLine.Inputs[1].UpdateTypeAndTextboxVisibility(internalMethodCall.Outputs[1].Type, overrideInitialType: true);
		graph.AddNode(writeLine, false);

		// Connect the internal method call to the return node
		graph.Connect(internalMethodCall.Outputs[0], writeLine.Inputs[0], false);
		graph.Connect(writeLine.Outputs[0], returnNode.Inputs[0], false);

		// Connect the output of the internal method call to the return node
		graph.Connect(internalMethodCall.Outputs[1], writeLine.Inputs[1], false);
		graph.Connect(internalMethodCall.Outputs[1], returnNode.Inputs[1], false);


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

			string? line= null;
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
			Directory.Delete(buildOptions.OutputPath, true);
		}
	}

	public static TheoryData<SerializableBuildOptions> GetBuildOptions() => new([new(true), new(false)]);

	[Theory]
	[MemberData(nameof(GetBuildOptions))]
	public void SimpleAdd(SerializableBuildOptions options)
	{
		var graph = CreateSimpleAddGraph<int, int>(out _, out _, out _);

		var output = graph.Project.Run(options, [1, 2]);

		Assert.Equal(3, output);
	}

	[Theory]
	[MemberData(nameof(GetBuildOptions))]
	public void SimpleAdd_CheckTypeFloat(SerializableBuildOptions options)
	{
		var graph = CreateSimpleAddGraph<float, float>(out _, out _, out _);

		var output = graph.Project.Run(options, [1.5f, 2f]);

		Assert.Equal(3.5f, output);
	}

	[Theory]
	[MemberData(nameof(GetBuildOptions))]
	public void TestBranch(SerializableBuildOptions options)
	{
		var graph = CreateSimpleAddGraph<int, int>(out var entryNode, out var returnNode1, out var addNode);
		graph.Disconnect(entryNode.Outputs[0], returnNode1.Inputs[0], false);
		graph.Disconnect(returnNode1.Inputs[1], addNode.Outputs[0], false);
		returnNode1.Inputs[1].UpdateTextboxText("1");

		var smallerThan = new Core.Nodes.Math.SmallerThan(graph);
		smallerThan.Inputs[0].UpdateTypeAndTextboxVisibility(graph.SelfClass.TypeFactory.Get<int>(), overrideInitialType: true);
		smallerThan.Inputs[1].UpdateTypeAndTextboxVisibility(graph.SelfClass.TypeFactory.Get<int>(), overrideInitialType: true);
		smallerThan.Inputs[1].UpdateTextboxText("0");
		graph.AddNode(smallerThan, false);
		graph.Connect(addNode.Outputs[0], smallerThan.Inputs[0], false);

		var returnNode2 = new Core.Nodes.Flow.ReturnNode(graph);
		returnNode2.Inputs.Add(new("Result", entryNode, graph.SelfClass.TypeFactory.Get<int>()));
		returnNode2.Inputs[1].UpdateTextboxText("0");
		graph.AddNode(returnNode2, false);

		var branchNode = new Core.Nodes.Flow.Branch(graph);
		graph.Connect(entryNode.Outputs[0], branchNode.Inputs[0], false);
		graph.Connect(smallerThan.Outputs[0], branchNode.Inputs[1], false);
		graph.AddNode(branchNode, false);

		graph.Connect(branchNode.Outputs[0], returnNode1.Inputs[0], false);
		graph.Connect(branchNode.Outputs[1], returnNode2.Inputs[0], false);

		var output = graph.Project.Run(options, [1, 2]);
		Assert.Equal(0, output);

		output = graph.Project.Run(options, [1, -2]);
		Assert.Equal(1, output);
	}

	[Theory]
	[MemberData(nameof(GetBuildOptions))]
	public void TestProjectRun(SerializableBuildOptions options)
	{
		var graph = CreateSimpleAddGraph<int, int>(out var entryNode, out var returnNode1, out var addNode);
		graph.Disconnect(entryNode.Outputs[0], returnNode1.Inputs[0], false);
		graph.Disconnect(returnNode1.Inputs[1], addNode.Outputs[0], false);
		returnNode1.Inputs[1].UpdateTextboxText("1");

		var smallerThan = new Core.Nodes.Math.SmallerThan(graph);
		graph.AddNode(smallerThan, false);
		smallerThan.Inputs[0].UpdateTypeAndTextboxVisibility(graph.SelfClass.TypeFactory.Get<int>(), overrideInitialType: true);
		smallerThan.Inputs[1].UpdateTypeAndTextboxVisibility(graph.SelfClass.TypeFactory.Get<int>(), overrideInitialType: true);
		smallerThan.Inputs[1].UpdateTextboxText("0");
		graph.Connect(addNode.Outputs[0], smallerThan.Inputs[0], false);

		var returnNode2 = new Core.Nodes.Flow.ReturnNode(graph);
		graph.AddNode(returnNode2, false);
		returnNode2.Inputs.Add(new("Result", entryNode, graph.SelfClass.TypeFactory.Get<int>()));
		returnNode2.Inputs[1].UpdateTextboxText("0");

		var branchNode = new Core.Nodes.Flow.Branch(graph);
		graph.AddNode(branchNode, false);
		graph.Connect(entryNode.Outputs[0], branchNode.Inputs[0], false);
		graph.Connect(smallerThan.Outputs[0], branchNode.Inputs[1], false);

		graph.Connect(branchNode.Outputs[0], returnNode1.Inputs[0], false);
		graph.Connect(branchNode.Outputs[1], returnNode2.Inputs[0], false);

		var output = graph.SelfClass.Project.Run(options, [1, 2]);

		Assert.Equal(0, output);

		output = graph.SelfClass.Project.Run(options, [-1, -2]);
		Assert.Equal(1, output);
	}
}