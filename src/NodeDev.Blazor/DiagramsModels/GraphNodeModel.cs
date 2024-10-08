﻿using Blazor.Diagrams.Core.Models;
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

		internal void OnNodeExecuted(Connection exec)
		{

		}

        internal void OnConnectionPathHighlighted(Connection connection)
		{
			var port = GetPort(connection);

			foreach (var link in port.Links.OfType<LinkModel>())
			{
				if (!link.Classes.Contains("highlighted"))
					link.Classes += " highlighted";

				link.Refresh();
			}
		}

        internal void OnConnectionPathUnhighlighted(Connection connection)
		{
			var port = GetPort(connection);

			foreach (var link in port.Links.OfType<LinkModel>())
			{
				link.Classes = link.Classes.Replace(" highlighted", "");
				link.Refresh();
			}
		}

		internal async Task OnNodeExecuting(Connection exec)
		{
            var port = GetPort(exec);

            foreach (var link in port.Links.OfType<LinkModel>())
            {
                if(!link.Classes.Contains("executing"))
                    link.Classes += " executing";

                link.Refresh();
            }

            var currentCount = ++port.ExecutionCount;
            await Task.Delay(100);
            if (currentCount == port.ExecutionCount)
            {
                foreach (var link in port.Links.OfType<LinkModel>())
                {
                    link.Classes = link.Classes.Replace(" executing", "");
					link.Refresh();
				}
			}
		}
	}
}
