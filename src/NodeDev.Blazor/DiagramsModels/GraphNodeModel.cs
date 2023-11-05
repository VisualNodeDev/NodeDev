using Blazor.Diagrams.Core.Models;
using NodeDev.Blazor.NodeAttributes;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Blazor.DiagramsModels
{
    public class GraphNodeModel : NodeModel
    {
        internal readonly Node Node;

        public GraphNodeModel(Node node) : base(new(node.GetOrAddDecoration<NodeDecorationPosition>(() => new(Vector2.Zero)).X, node.GetOrAddDecoration<NodeDecorationPosition>(() => new(Vector2.Zero)).Y))
        {
            Node = node;
        }

        public GraphPortModel GetPort(Connection connection) => Ports.OfType<GraphPortModel>().First( x=> x.Connection == connection);

        internal void UpdateNodeBaseInfo(Node node)
        {

        }
    }
}
