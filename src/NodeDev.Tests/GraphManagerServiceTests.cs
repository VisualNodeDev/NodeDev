using NodeDev.Blazor.Services.GraphManager;
using NodeDev.Core;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Tests;

public class GraphManagerServiceTests : NodeDevTestsBase
{
    [Fact]
    public void ConnectTwoExecInOneOutput_ShouldDisconnectFirstExec()
    {
        var project = Project.CreateNewDefaultProject(out var main);
        Assert.NotNull(main.EntryNode);
        Assert.Single(main.ReturnNodes);
        Assert.Equal(main.EntryNode.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);

        // create fake IGraphCanvas
        var graphCanvas = Substitute.For<IGraphCanvas>();
        graphCanvas.Graph.Returns(main.Graph);

        var graphManager = new GraphManagerService(graphCanvas);

        // create a random method call used to test the connection
        var methodCall = new MethodCall(main.Graph);
        main.Graph.AddNode(methodCall, false);


        // This should also disconnect the entry node's existing exec connection
        graphManager.AddNewConnectionBetween(main.EntryNode.Outputs[0], methodCall.Inputs[0]);

        // main entry node was disconnected from the other node and is now connected to the method call
        Assert.Single(main.EntryNode.Outputs[0].Connections);
        Assert.Equal(main.EntryNode.Outputs[0].Connections[0], methodCall.Inputs[0]);

        // return node is not connected to anything
        Assert.Empty(main.ReturnNodes.Single().Inputs[0].Connections);

        // check that each connection was updated
        graphCanvas.Received().UpdatePortTypeAndColor(Arg.Is(main.EntryNode.Outputs[0].Connections[0]));
        graphCanvas.Received().UpdatePortTypeAndColor(Arg.Is(methodCall.Inputs[0]));
        graphCanvas.Received().UpdatePortTypeAndColor(Arg.Is(main.ReturnNodes.Single().Inputs[0]));

        // check that the old connection was removed from the graph canvas
        graphCanvas.Received().RemoveLinkFromGraphCanvas(Arg.Is(main.EntryNode.Outputs[0]), Arg.Is(main.ReturnNodes.Single().Inputs[0]));
    }

    [Fact]
    public void ConnectTwoOutputsInOneInput_ShouldDisconnectFirstOutput()
    {
        var project = Project.CreateNewDefaultProject(out var main);
        Assert.NotNull(main.EntryNode);
        Assert.Single(main.ReturnNodes);
        Assert.Equal(main.EntryNode.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);

        // create fake IGraphCanvas
        var graphCanvas = Substitute.For<IGraphCanvas>();
        graphCanvas.Graph.Returns(main.Graph);

        var graphManager = new GraphManagerService(graphCanvas);

        var addNode1 = AddNewAddNodeToGraph<int>(main.Graph);
        var addNode2 = AddNewAddNodeToGraph<int>(main.Graph);
        var addNode3 = AddNewAddNodeToGraph<int>(main.Graph);

        // connect output of addNode1 to input of addNode3
        graphManager.AddNewConnectionBetween(addNode1.Outputs[0], addNode3.Inputs[0]);
        graphCanvas.Received().UpdatePortTypeAndColor(Arg.Is(addNode1.Outputs[0]));
        graphCanvas.Received().UpdatePortTypeAndColor(Arg.Is(addNode3.Inputs[0]));
        Assert.Single(addNode1.Outputs[0].Connections);
        Assert.Single(addNode3.Inputs[0].Connections);
        Assert.Equal(addNode1.Outputs[0].Connections[0], addNode3.Inputs[0]);

        // connect output of addNode2 to input of addNode3. It should disconnect the existing connection
        graphManager.AddNewConnectionBetween(addNode2.Outputs[0], addNode3.Inputs[0]);
        graphCanvas.Received(2).UpdatePortTypeAndColor(Arg.Is(addNode1.Outputs[0])); // when first adding, then when disconnecting
        graphCanvas.Received(1).UpdatePortTypeAndColor(Arg.Is(addNode2.Outputs[0]));
        graphCanvas.Received(3).UpdatePortTypeAndColor(Arg.Is(addNode3.Inputs[0])); // when first adding, adding a second time, then disconnecting
        Assert.Empty(addNode1.Outputs[0].Connections);
        Assert.Single(addNode2.Outputs[0].Connections);
        Assert.Single(addNode3.Inputs[0].Connections);
        Assert.Equal(addNode2.Outputs[0].Connections[0], addNode3.Inputs[0]);

        graphCanvas.Received().RemoveLinkFromGraphCanvas(Arg.Is(addNode1.Outputs[0]), Arg.Is(addNode3.Inputs[0]));
    }

    [Fact]
    public void ConnectTwoExecOutputsInOneInput_ShouldAllow()
    {
        var project = Project.CreateNewDefaultProject(out var main);
        Assert.NotNull(main.EntryNode);
        Assert.Single(main.ReturnNodes);
        Assert.Equal(main.EntryNode.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);

        // create fake IGraphCanvas
        var graphCanvas = Substitute.For<IGraphCanvas>();
        graphCanvas.Graph.Returns(main.Graph);

        var graphManager = new GraphManagerService(graphCanvas);

        // create a random method call used to test the connection
        var methodCall = new MethodCall(main.Graph);
        main.Graph.AddNode(methodCall, false);

        // connect output of addNode1 to input of addNode3
        graphManager.AddNewConnectionBetween(methodCall.Outputs[0], main.ReturnNodes.Single().Inputs[0]);
        graphCanvas.Received().UpdatePortTypeAndColor(Arg.Is(methodCall.Outputs[0]));
        graphCanvas.Received().UpdatePortTypeAndColor(Arg.Is(main.ReturnNodes.Single().Inputs[0]));
        Assert.Single(main.EntryNode.Outputs[0].Connections);
        Assert.Equal(2, main.ReturnNodes.Single().Inputs[0].Connections.Count);
        Assert.Single(methodCall.Outputs[0].Connections);
        Assert.Equal(methodCall.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);
        graphCanvas.DidNotReceiveWithAnyArgs().RemoveLinkFromGraphCanvas(Arg.Any<Connection>(), Arg.Any<Connection>());
    }

    [Fact]
    public void ConnectArrayToIEnumerableT_ShouldAllow()
    {
        var project = Project.CreateNewDefaultProject(out var main);
        Assert.NotNull(main.EntryNode);
        Assert.Single(main.ReturnNodes);
        Assert.Equal(main.EntryNode.Outputs[0].Connections[0], main.ReturnNodes.Single().Inputs[0]);

        var typeFactory = main.TypeFactory;

        // create fake IGraphCanvas
        var graphCanvas = Substitute.For<IGraphCanvas>();
        graphCanvas.Graph.Returns(main.Graph);

        var graphManager = new GraphManagerService(graphCanvas);

        // create a random method call used to test the connection
        var methodCall = AddMethodCall(main.Graph, typeFactory.Get<Array>(), nameof(Array.Empty));
        methodCall.Outputs[1].UpdateTypeAndTextboxVisibility(typeFactory.Get<string[]>());

        var foreachNode = new ForeachNode(main.Graph);
        main.Graph.AddNode(foreachNode, false);

        // connect output of Array.Empty<string>() to input of foreachNode
        graphManager.AddNewConnectionBetween(methodCall.Outputs[1], foreachNode.Inputs[1]);
        graphCanvas.Received().UpdatePortTypeAndColor(Arg.Is(methodCall.Outputs[1]));
        graphCanvas.Received().UpdatePortTypeAndColor(Arg.Is(foreachNode.Inputs[1]));
        graphCanvas.Received().UpdatePortTypeAndColor(Arg.Is(foreachNode.Outputs[1]));
        Assert.Equal(typeFactory.Get<IEnumerable<string>>(), foreachNode.Inputs[1].Type);
        Assert.Equal(typeFactory.Get<string>(), foreachNode.Outputs[1].Type);
    }
}
