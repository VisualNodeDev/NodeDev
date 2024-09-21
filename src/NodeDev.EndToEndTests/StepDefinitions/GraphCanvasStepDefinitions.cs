using Microsoft.Playwright;
using NodeDev.EndToEndTests.Pages;

namespace NodeDev.EndToEndTests.StepDefinitions;

[Binding]
public class GraphCanvasStepDefinitions
{
    private readonly IPage User;
    private readonly GraphCanvasPage GraphCanvasPage;
    private readonly NodeSelectionPage NodeSelectionPage;

    public GraphCanvasStepDefinitions(Hooks.Hooks hooks, GraphCanvasPage graphCanvasPage, NodeSelectionPage nodeSelectionPage)
    {
        User = hooks.User;
        GraphCanvasPage = graphCanvasPage;
        NodeSelectionPage = nodeSelectionPage;
    }

    [Given("I add a node of type {string} from connection {string} of existing node {string}")]
    public async Task GivenIAddANodeOfTypeFromConnectionOfExistingNode(string nodeType, string existingConnectionName, string existingNodeName)
    {
        var nodePort = await GraphCanvasPage.GetGraphNodeConnectionByName(existingNodeName, existingConnectionName);

        await GraphCanvasPage.OpenNodeSelection(nodePort);

        await NodeSelectionPage.SearchAndAcceptByText("New");

        await Task.Delay(10000);
    }
}
