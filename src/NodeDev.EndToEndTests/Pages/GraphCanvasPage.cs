using Microsoft.Playwright;

namespace NodeDev.EndToEndTests.Pages;

public class GraphCanvasPage
{
    private readonly IPage User;
    private readonly NodeSelectionPage NodeSelectionPage;

    public GraphCanvasPage(Hooks.Hooks hooks, NodeSelectionPage nodeSelectionPage)
    {
        User = hooks.User;
        NodeSelectionPage = nodeSelectionPage;
    }

    private ILocator SearchGraphCanvas => User.Locator("[data-test-id='graphCanvas']");

    private ILocator SearchGraphNode => SearchGraphCanvas.Locator("[data-test-id='graphNode']");

    public async Task<ILocator> GetGraphNodeByName(string name)
    {
        var node = SearchGraphNode.Filter(new()
        {
            Has = User.Locator(".nodeName").GetByText(name)
        });
        await node.WaitForVisible();

        return node;
    }

    public async Task<ILocator> GetGraphNodeConnectionByName(string nodeName, string connection)
    {
        var node = await GetGraphNodeByName(nodeName);

        return await GetGraphNodeConnectionByName(node, connection);
    }

    public async Task OpenNodeSelection(ILocator nodeConnection)
    {
        await nodeConnection.DispatchEventAsync("pointerdown");

        // get the position of the node and move the mouse on it
        // This isn't necessary for the pointdown even to work BUT it is necessary for the graph since it locally stores the last mouse position
        var boundingBox = (await nodeConnection.BoundingBoxAsync())!;
        await User.Mouse.MoveAsync(boundingBox.X + boundingBox.Width / 2, boundingBox.Y + boundingBox.Height / 2);

        await nodeConnection.DispatchEventAsync("pointerup");
    }

    public async Task<ILocator> GetGraphNodeConnectionByName(ILocator node, string name)
    {
        var port = node.Locator("[data-test-id='graphPort']").Filter(new()
        {
            Has = User.Locator(".name").GetByText(name)
        }).Locator(".diagram-port");

        await port.WaitForVisible();

        return port;
    }
}