using NodeDev.Core.Nodes.Flow;
using System.Linq.Expressions;

namespace NodeDev.Core;

public class GraphExpressionGenerator
{


    public Expression GenerateExpression(Graph graph)
    {
        var entry = graph.Nodes.Values.OfType<EntryNode>().First();



        return null;
    }

}
