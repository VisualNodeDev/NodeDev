using NodeDev.Core.Nodes;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Connections
{
	public class Connection
	{
		public string Id { get; }

		public string Name { get; set; }

		public Node Parent { get; }

		public TypeBase Type { get; }

		public ICollection<Connection> Connections { get; } = new List<Connection>();

		public Connection(string name, Node parent, TypeBase type, string? id = null)
		{
			Id = id ?? Guid.NewGuid().ToString();
			Name = name;
			Parent = parent;
			Type = type;
		}
	}
}
