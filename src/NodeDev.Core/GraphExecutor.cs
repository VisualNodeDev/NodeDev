using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core
{
    public class GraphExecutor
    {
        public readonly Graph Graph;

        private readonly Dictionary<Connection, object?> Connections = new();

        public GraphExecutor(Graph graph)
        {
            Graph = graph;
        }

        private Node FindEntryNode()
        {
            return Graph.Nodes.First(n => n.Value is EntryNode).Value;
        }

        public void Execute(object?[] inputs, object?[] outputs)
        {
            var node = FindEntryNode();

            var execConnection = node.Execute(null, inputs, inputs);
            if (execConnection == null)
                throw new Exception("Entry node should have an output connection");

            var stack = new Stack<Connection>(); // stack of input connections on nodes that can alter execution path

            while (true)
            {
                var connectionToExecute = execConnection?.Connections.FirstOrDefault();

                if(connectionToExecute == null) // pop the stack
                {
                    if (!stack.TryPop(out var previousInputExec))
                        break;

                    connectionToExecute = previousInputExec;
                }

                var nodeToExecute = connectionToExecute.Parent;

                if (nodeToExecute is ReturnNode) // we are done
                {
                    for(int i = 0; i < outputs.Length; i++)
                    {
                        var output = nodeToExecute.Inputs[i];
                        outputs[i] = CrawlBackInputs(output);
                    }
                    return;
                }

                var nodeInputs = GetNodeInputs(nodeToExecute);
                var nodeOutputs = new object?[nodeToExecute.Outputs.Count];

                execConnection = nodeToExecute.Execute(connectionToExecute, nodeInputs, nodeOutputs);
                for(int i = 0; i < nodeOutputs.Length; i++)
                {
                    var output = nodeToExecute.Outputs[i];
                    Connections[output] = nodeOutputs[i];
                }

                if (execConnection != null && nodeToExecute.AlterExecutionStackOnPop)
                    stack.Push(connectionToExecute);
            }
        }

        private object?[] GetNodeInputs(Node node)
        {
            var inputs = new object?[node.Inputs.Count];

            for (int i = 0; i < node.Inputs.Count; i++)
            {
                var input = node.Inputs[i];

                inputs[i] = CrawlBackInputs(input);
            }

            return inputs;
        }

        private object? CrawlBackInputs(Connection inputConnection)
        {
            var other = inputConnection.Connections.FirstOrDefault();
            if (other == null)
                return inputConnection.ParsedTextboxValue;

            if(other.Parent.IsFlowNode) // we can stop crawling back and just get the value of the input
                return Connections.TryGetValue(other, out var v) ? v : null;


            // if this is not a flow node, we are allowed to execute the node on demande to calculate the outputs
            // this will also automatically crawl back the inputs of the node we are executing
            var inputs = GetNodeInputs(other.Parent);
            var outputs = new object?[other.Parent.Outputs.Count];

            object? myOutput = null;
            other.Parent.Execute(null, inputs, outputs);
            for (int i = 0; i < outputs.Length; i++)
            {
                var output = other.Parent.Outputs[i];
                Connections[output] = outputs[i];

                if(output == other)
                    myOutput = outputs[i];
            }

            return myOutput;
        }
    }
}
