using NodeDev.Core;
using NodeDev.Core.Class;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Delegates;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Nodes.Math;

namespace NodeDev.Tests;

public class LambdaRegionTests
{
	[Fact]
	public void CreateFunc_DefaultsResultTypeToBool()
	{
		var (_, method, _) = CreateMethod<int>("Run");
		var graph = method.Graph;
		var func = AddDelegate<CreateFuncNode>(graph);
		var lambdaReturn = Assert.Single(graph.GetNodesInScope(func.BodyScopeId).OfType<LambdaReturnNode>());

		Assert.Equal(graph.Project.TypeFactory.Get<bool>(), func.ResultType);
		Assert.Equal(typeof(Func<bool>), func.DelegateType.MakeRealType());
		Assert.Equal(func.ResultType, lambdaReturn.ResultInput.Type);
	}

	[Fact]
	public void CreateFunc_CreatesScopedEntryAndReturn_AndProjectsSignature()
	{
		var (_, method, _) = CreateMethod<int, int>("Run", "value");
		var graph = method.Graph;
		var func = AddDelegate<CreateFuncNode>(graph);

		func.SetResultType(graph.Project.TypeFactory.Get<int>());
		var parameter = func.AddParameter("item", graph.Project.TypeFactory.Get<int>());
		var capture = func.AddCapture("prefix", graph.Project.TypeFactory.Get<string>());

		var body = graph.GetNodesInScope(func.BodyScopeId).ToList();
		var entry = Assert.Single(body.OfType<LambdaEntryNode>());
		var lambdaReturn = Assert.Single(body.OfType<LambdaReturnNode>());

		Assert.Equal(func.Id, entry.CallableScopeId);
		Assert.Equal(func.Id, lambdaReturn.CallableScopeId);
		Assert.True(lambdaReturn.IsImplicit);
		Assert.Equal("item", Assert.Single(entry.ParameterOutputs).Name);
		Assert.Equal("Captured prefix", Assert.Single(entry.CaptureOutputs).Name);
		Assert.Equal(parameter.Type, entry.ParameterOutputs[0].Type);
		Assert.Equal(capture.Type, func.CaptureInputs[0].Type);
		Assert.Equal(func.ResultType, lambdaReturn.ResultInput.Type);
		Assert.Equal(typeof(Func<int, int>), func.DelegateType.MakeRealType());
	}

	[Fact]
	public void CrossScopeConnection_IsRejectedWithoutMutation()
	{
		var (_, method, entry) = CreateMethod<int, int>("Run", "value");
		var graph = method.Graph;
		var func = AddDelegate<CreateFuncNode>(graph);
		func.SetResultType(graph.Project.TypeFactory.Get<int>());
		var lambdaReturn = Assert.Single(graph.GetNodesInScope(func.BodyScopeId).OfType<LambdaReturnNode>());

		var error = Assert.Throws<InvalidOperationException>(() =>
			graph.Manager.AddNewConnectionBetween(entry.Outputs[1], lambdaReturn.ResultInput));

		Assert.Contains("scope boundary", error.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Empty(entry.Outputs[1].Connections);
		Assert.Empty(lambdaReturn.ResultInput.Connections);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void FuncParameter_CanBeInvoked_AndDoesNotReturnFromContainingMethod(bool debug)
	{
		var (project, method, entry) = CreateMethod<int, int>("Double", "value");
		var graph = method.Graph;
		var methodReturn = Assert.Single(method.ReturnNodes);
		var func = AddDelegate<CreateFuncNode>(graph);
		func.SetResultType(graph.Project.TypeFactory.Get<int>());
		func.AddParameter("item", graph.Project.TypeFactory.Get<int>());

		var lambdaEntry = Assert.Single(graph.GetNodesInScope(func.BodyScopeId).OfType<LambdaEntryNode>());
		var lambdaReturn = Assert.Single(graph.GetNodesInScope(func.BodyScopeId).OfType<LambdaReturnNode>());
		var add = new Add(graph);
		graph.Manager.AddNode(add, func.BodyScopeId);
		graph.Manager.AddNewConnectionBetween(lambdaEntry.ParameterOutputs[0], add.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(lambdaEntry.ParameterOutputs[0], add.Inputs[1]);
		graph.Manager.AddNewConnectionBetween(add.Outputs[0], lambdaReturn.ResultInput);

		var invoke = AddTypedInvoke(graph, func.DelegateType);
		graph.Manager.AddNewConnectionBetween(func.DelegateOutput, invoke.DelegateInput);
		graph.Manager.AddNewConnectionBetween(entry.Outputs[1], invoke.InvocationInputs[0]);
		graph.Manager.AddNewConnectionBetween(entry.Outputs[0], invoke.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(invoke.Outputs[0], methodReturn.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(invoke.ResultOutput!, methodReturn.Inputs[^1]);

		var result = new RoslynNodeClassCompiler(project, debug ? BuildOptions.Debug : BuildOptions.Release).Compile();
		var generatedMethod = result.Assembly.GetType("LambdaTests.TestClass")!.GetMethod("Double")!;

		Assert.Equal(12, generatedMethod.Invoke(null, [6]));
		Assert.Contains("global::System.Func<int, int>", result.SourceCode);
		Assert.Contains("(int item) =>", result.SourceCode);
	}

	[Fact]
	public void CrossScopeDataConnection_AutomaticallyCreatesSnapshottedCapture()
	{
		var (project, method, entry) = CreateMethod<int, int>("Capture", "value");
		var graph = method.Graph;
		var methodReturn = Assert.Single(method.ReturnNodes);
		var func = AddDelegate<CreateFuncNode>(graph);
		func.SetResultType(graph.Project.TypeFactory.Get<int>());

		var lambdaEntry = Assert.Single(graph.GetNodesInScope(func.BodyScopeId).OfType<LambdaEntryNode>());
		var lambdaReturn = Assert.Single(graph.GetNodesInScope(func.BodyScopeId).OfType<LambdaReturnNode>());
		graph.Manager.AddNewConnectionBetweenOrCapture(entry.Outputs[1], lambdaReturn.ResultInput);

		Assert.Equal("value", Assert.Single(func.Captures).Name, ignoreCase: true);
		Assert.Contains(func.CaptureInputs[0], entry.Outputs[1].Connections);
		Assert.Contains(lambdaReturn.ResultInput, lambdaEntry.CaptureOutputs[0].Connections);

		var invoke = AddTypedInvoke(graph, func.DelegateType);
		graph.Manager.AddNewConnectionBetween(func.DelegateOutput, invoke.DelegateInput);
		graph.Manager.AddNewConnectionBetween(entry.Outputs[0], invoke.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(invoke.Outputs[0], methodReturn.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(invoke.ResultOutput!, methodReturn.Inputs[^1]);

		var result = new RoslynNodeClassCompiler(project, BuildOptions.Release).Compile();
		var generatedMethod = result.Assembly.GetType("LambdaTests.TestClass")!.GetMethod("Capture")!;

		Assert.Equal(42, generatedMethod.Invoke(null, [42]));
		Assert.Contains("lambdaCapture_value", result.SourceCode);
	}

	[Fact]
	public void Action_CanBeCreatedAndInvoked()
	{
		var project = new Project(Guid.NewGuid());
		var nodeClass = new NodeClass("TestClass", "LambdaTests", project);
		project.AddClass(nodeClass);
		var method = new NodeClassMethod(nodeClass, "RunAction", project.TypeFactory.Void) { IsStatic = true };
		nodeClass.AddMethod(method, createEntryAndReturn: false);
		method.Parameters.Add(new NodeClassMethodParameter("value", project.TypeFactory.Get<int>(), method));
		var entry = new EntryNode(method.Graph);
		var methodReturn = new ReturnNode(method.Graph);
		method.Graph.Manager.AddNode(entry);
		method.Graph.Manager.AddNode(methodReturn);
		var action = AddDelegate<CreateActionNode>(method.Graph);
		action.AddParameter("value", project.TypeFactory.Get<int>());
		var invoke = AddTypedInvoke(method.Graph, action.DelegateType);

		method.Graph.Manager.AddNewConnectionBetween(action.DelegateOutput, invoke.DelegateInput);
		method.Graph.Manager.AddNewConnectionBetween(entry.Outputs[1], invoke.InvocationInputs[0]);
		method.Graph.Manager.AddNewConnectionBetween(entry.Outputs[0], invoke.Inputs[0]);
		method.Graph.Manager.AddNewConnectionBetween(invoke.Outputs[0], methodReturn.Inputs[0]);

		var result = new RoslynNodeClassCompiler(project, BuildOptions.Release).Compile();
		var generatedMethod = result.Assembly.GetType("LambdaTests.TestClass")!.GetMethod("RunAction")!;
		generatedMethod.Invoke(null, [42]);

		Assert.Contains("global::System.Action<int>", result.SourceCode);
		Assert.DoesNotContain("Invoke_Delegate_Result", result.SourceCode);
	}

	[Fact]
	public void FuncBranch_AllowsMultipleLambdaReturns()
	{
		var (project, method, entry) = CreateMethod<int, bool>("Choose", "flag");
		var graph = method.Graph;
		var methodReturn = Assert.Single(method.ReturnNodes);
		var func = AddDelegate<CreateFuncNode>(graph);
		func.SetResultType(graph.Project.TypeFactory.Get<int>());
		func.AddParameter("flag", graph.Project.TypeFactory.Get<bool>());
		var lambdaEntry = Assert.Single(graph.GetNodesInScope(func.BodyScopeId).OfType<LambdaEntryNode>());
		var returnTrue = Assert.Single(graph.GetNodesInScope(func.BodyScopeId).OfType<LambdaReturnNode>());
		returnTrue.ResultInput.UpdateTextboxText("1");
		var returnFalse = new LambdaReturnNode(graph);
		Assert.False(returnFalse.IsImplicit);
		returnFalse.ResultInput.UpdateTypeAndTextboxVisibility(graph.Project.TypeFactory.Get<int>(), overrideInitialType: true);
		returnFalse.ResultInput.UpdateTextboxText("2");
		graph.Manager.AddNode(returnFalse, func.BodyScopeId);
		var branch = new Branch(graph);
		graph.Manager.AddNode(branch, func.BodyScopeId);

		graph.Manager.AddNewConnectionBetween(lambdaEntry.ExecOutput, branch.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(lambdaEntry.ParameterOutputs[0], branch.Inputs[1]);
		graph.Manager.AddNewConnectionBetween(branch.Outputs[0], returnTrue.ExecInput);
		graph.Manager.AddNewConnectionBetween(branch.Outputs[1], returnFalse.ExecInput);

		var invoke = AddTypedInvoke(graph, func.DelegateType);
		graph.Manager.AddNewConnectionBetween(func.DelegateOutput, invoke.DelegateInput);
		graph.Manager.AddNewConnectionBetween(entry.Outputs[1], invoke.InvocationInputs[0]);
		graph.Manager.AddNewConnectionBetween(entry.Outputs[0], invoke.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(invoke.Outputs[0], methodReturn.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(invoke.ResultOutput!, methodReturn.Inputs[^1]);

		var result = new RoslynNodeClassCompiler(project, BuildOptions.Release).Compile();
		var generatedMethod = result.Assembly.GetType("LambdaTests.TestClass")!.GetMethod("Choose")!;
		Assert.Equal(1, generatedMethod.Invoke(null, [true]));
		Assert.Equal(2, generatedMethod.Invoke(null, [false]));
	}

	[Fact]
	public void NestedFunc_CompilesAndRuns()
	{
		var (project, method, entry) = CreateMethod<int>("Nested");
		var graph = method.Graph;
		var methodReturn = Assert.Single(method.ReturnNodes);
		var outer = AddDelegate<CreateFuncNode>(graph);
		outer.SetResultType(graph.Project.TypeFactory.Get<int>());
		var outerEntry = Assert.Single(graph.GetNodesInScope(outer.BodyScopeId).OfType<LambdaEntryNode>());
		var outerReturn = Assert.Single(graph.GetNodesInScope(outer.BodyScopeId).OfType<LambdaReturnNode>());

		var inner = AddDelegate<CreateFuncNode>(graph, outer.BodyScopeId);
		inner.SetResultType(graph.Project.TypeFactory.Get<int>());
		var innerReturn = Assert.Single(graph.GetNodesInScope(inner.BodyScopeId).OfType<LambdaReturnNode>());
		innerReturn.ResultInput.UpdateTextboxText("7");

		var invokeInner = AddTypedInvoke(graph, inner.DelegateType, outer.BodyScopeId);
		graph.Manager.AddNewConnectionBetween(inner.DelegateOutput, invokeInner.DelegateInput);
		graph.Manager.AddNewConnectionBetween(outerEntry.ExecOutput, invokeInner.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(invokeInner.Outputs[0], outerReturn.ExecInput);
		graph.Manager.AddNewConnectionBetween(invokeInner.ResultOutput!, outerReturn.ResultInput);

		var invokeOuter = AddTypedInvoke(graph, outer.DelegateType);
		graph.Manager.AddNewConnectionBetween(outer.DelegateOutput, invokeOuter.DelegateInput);
		graph.Manager.AddNewConnectionBetween(entry.Outputs[0], invokeOuter.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(invokeOuter.Outputs[0], methodReturn.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(invokeOuter.ResultOutput!, methodReturn.Inputs[^1]);

		var result = new RoslynNodeClassCompiler(project, BuildOptions.Release).Compile();
		var generatedMethod = result.Assembly.GetType("LambdaTests.TestClass")!.GetMethod("Nested")!;

		Assert.Equal(7, generatedMethod.Invoke(null, null));
		Assert.Equal(2, CountOccurrences(result.SourceCode, "global::System.Func<int>"));
	}

	[Fact]
	public void AutomaticCapture_TraversesEveryNestedLambdaBoundary()
	{
		var (_, method, entry) = CreateMethod<int, int>("NestedCapture", "value");
		var graph = method.Graph;
		var outer = AddDelegate<CreateFuncNode>(graph);
		outer.SetResultType(graph.Project.TypeFactory.Get<int>());
		var inner = AddDelegate<CreateFuncNode>(graph, outer.BodyScopeId);
		inner.SetResultType(graph.Project.TypeFactory.Get<int>());
		var outerEntry = Assert.Single(graph.GetNodesInScope(outer.BodyScopeId).OfType<LambdaEntryNode>());
		var innerEntry = Assert.Single(graph.GetNodesInScope(inner.BodyScopeId).OfType<LambdaEntryNode>());
		var innerReturn = Assert.Single(graph.GetNodesInScope(inner.BodyScopeId).OfType<LambdaReturnNode>());

		graph.Manager.AddNewConnectionBetweenOrCapture(entry.Outputs[1], innerReturn.ResultInput);

		Assert.Single(outer.Captures);
		Assert.Single(inner.Captures);
		Assert.Contains(outer.CaptureInputs[0], entry.Outputs[1].Connections);
		Assert.Contains(inner.CaptureInputs[0], outerEntry.CaptureOutputs[0].Connections);
		Assert.Contains(innerReturn.ResultInput, innerEntry.CaptureOutputs[0].Connections);
	}

	[Fact]
	public void AutomaticCapture_RejectsSiblingScopesWithoutMutation()
	{
		var (_, method, _) = CreateMethod<int>("SiblingCapture");
		var graph = method.Graph;
		var left = AddDelegate<CreateFuncNode>(graph);
		left.SetResultType(graph.Project.TypeFactory.Get<int>());
		var right = AddDelegate<CreateFuncNode>(graph);
		right.SetResultType(graph.Project.TypeFactory.Get<int>());
		var leftEntry = Assert.Single(graph.GetNodesInScope(left.BodyScopeId).OfType<LambdaEntryNode>());
		left.AddParameter("value", graph.Project.TypeFactory.Get<int>());
		var rightReturn = Assert.Single(graph.GetNodesInScope(right.BodyScopeId).OfType<LambdaReturnNode>());

		Assert.Throws<InvalidOperationException>(() =>
			graph.Manager.AddNewConnectionBetweenOrCapture(leftEntry.ParameterOutputs[0], rightReturn.ResultInput));

		Assert.Empty(left.Captures);
		Assert.Empty(right.Captures);
		Assert.Empty(leftEntry.ParameterOutputs[0].Connections);
		Assert.Empty(rightReturn.ResultInput.Connections);
	}

	[Fact]
	public void LambdaGraph_RoundTripsAndStillRuns()
	{
		var (project, method, entry) = CreateMethod<int, int>("RoundTrip", "value");
		var graph = method.Graph;
		var methodReturn = Assert.Single(method.ReturnNodes);
		var func = AddDelegate<CreateFuncNode>(graph);
		func.SetResultType(graph.Project.TypeFactory.Get<int>());
		func.AddCapture("captured", graph.Project.TypeFactory.Get<int>());
		var lambdaEntry = Assert.Single(graph.GetNodesInScope(func.BodyScopeId).OfType<LambdaEntryNode>());
		var lambdaReturn = Assert.Single(graph.GetNodesInScope(func.BodyScopeId).OfType<LambdaReturnNode>());
		graph.Manager.AddNewConnectionBetween(lambdaEntry.CaptureOutputs[0], lambdaReturn.ResultInput);
		graph.Manager.AddNewConnectionBetween(entry.Outputs[1], func.CaptureInputs[0]);

		var invoke = AddTypedInvoke(graph, func.DelegateType);
		graph.Manager.AddNewConnectionBetween(func.DelegateOutput, invoke.DelegateInput);
		graph.Manager.AddNewConnectionBetween(entry.Outputs[0], invoke.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(invoke.Outputs[0], methodReturn.Inputs[0]);
		graph.Manager.AddNewConnectionBetween(invoke.ResultOutput!, methodReturn.Inputs[^1]);

		var serialized = project.Serialize();
		var restored = Project.Deserialize(serialized);
		var restoredMethod = restored.Classes.Single().Methods.Single();
		var restoredFunc = Assert.Single(restoredMethod.Graph.Nodes.Values.OfType<CreateFuncNode>());
		var restoredReturn = Assert.Single(restoredMethod.Graph.GetNodesInScope(restoredFunc.BodyScopeId).OfType<LambdaReturnNode>());
		Assert.Equal("captured", Assert.Single(restoredFunc.Captures).Name);
		Assert.True(restoredReturn.IsImplicit);
		Assert.All(restoredMethod.Graph.GetNodesInScope(restoredFunc.BodyScopeId), node => Assert.Equal(restoredFunc.Id, node.CallableScopeId));

		var result = new RoslynNodeClassCompiler(restored, BuildOptions.Release).Compile();
		var generatedMethod = result.Assembly.GetType("LambdaTests.TestClass")!.GetMethod("RoundTrip")!;
		Assert.Equal(9, generatedMethod.Invoke(null, [9]));
	}

	[Fact]
	public void RemovingLambda_RemovesNestedScopesButPreservesRootSiblings()
	{
		var (_, method, entry) = CreateMethod<int>("Delete");
		var graph = method.Graph;
		var outer = AddDelegate<CreateFuncNode>(graph);
		outer.SetResultType(graph.Project.TypeFactory.Get<int>());
		var inner = AddDelegate<CreateActionNode>(graph, outer.BodyScopeId);
		var rootSibling = new Add(graph);
		graph.Manager.AddNode(rootSibling);

		graph.Manager.RemoveNode(outer);

		Assert.DoesNotContain(outer, graph.Nodes.Values);
		Assert.DoesNotContain(inner, graph.Nodes.Values);
		Assert.DoesNotContain(graph.Nodes.Values, node => node.CallableScopeId == outer.Id || node.CallableScopeId == inner.Id);
		Assert.Contains(entry, graph.Nodes.Values);
		Assert.Contains(rootSibling, graph.Nodes.Values);
	}

	private static T AddDelegate<T>(Graph graph, string? scopeId = null) where T : CreateDelegateNode
	{
		return (T)graph.Manager.AddNode(new NodeProvider.NodeSearchResult(typeof(T)), _ => { }, scopeId);
	}

	private static InvokeDelegateNode AddTypedInvoke(Graph graph, NodeDev.Core.Types.TypeBase delegateType, string? scopeId = null)
	{
		return (InvokeDelegateNode)graph.Manager.AddNode(
			new NodeProvider.DelegateInvocationNode(typeof(InvokeDelegateNode), delegateType),
			_ => { },
			scopeId);
	}

	private static (Project Project, NodeClassMethod Method, EntryNode Entry) CreateMethod<TResult>(string name)
	{
		var project = new Project(Guid.NewGuid());
		var nodeClass = new NodeClass("TestClass", "LambdaTests", project);
		project.AddClass(nodeClass);
		var method = new NodeClassMethod(nodeClass, name, project.TypeFactory.Get<TResult>()) { IsStatic = true };
		nodeClass.AddMethod(method, createEntryAndReturn: false);
		var entry = new EntryNode(method.Graph);
		var methodReturn = new ReturnNode(method.Graph);
		method.Graph.Manager.AddNode(entry);
		method.Graph.Manager.AddNode(methodReturn);
		method.Graph.Manager.AddNewConnectionBetween(entry.Outputs[0], methodReturn.Inputs[0]);
		return (project, method, entry);
	}

	private static (Project Project, NodeClassMethod Method, EntryNode Entry) CreateMethod<TResult, TParameter>(string name, string parameterName)
	{
		var project = new Project(Guid.NewGuid());
		var nodeClass = new NodeClass("TestClass", "LambdaTests", project);
		project.AddClass(nodeClass);
		var method = new NodeClassMethod(nodeClass, name, project.TypeFactory.Get<TResult>()) { IsStatic = true };
		nodeClass.AddMethod(method, createEntryAndReturn: false);
		method.Parameters.Add(new NodeClassMethodParameter(parameterName, project.TypeFactory.Get<TParameter>(), method));
		var entry = new EntryNode(method.Graph);
		var methodReturn = new ReturnNode(method.Graph);
		method.Graph.Manager.AddNode(entry);
		method.Graph.Manager.AddNode(methodReturn);
		method.Graph.Manager.AddNewConnectionBetween(entry.Outputs[0], methodReturn.Inputs[0]);
		return (project, method, entry);
	}

	private static int CountOccurrences(string text, string value)
	{
		var count = 0;
		var index = 0;
		while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
		{
			count++;
			index += value.Length;
		}
		return count;
	}
}
