using NodeDev.Core.Nodes;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NodeDev.Core.Connections
{
	public class Connection
	{
		public string Id { get; }

		public string Name { get; set; }

		public Node Parent { get; }

		public TypeBase Type { get; set; }

		public ICollection<Connection> Connections { get; } = new List<Connection>();

		public Connection(string name, Node parent, TypeBase type, string? id = null)
		{
			Id = id ?? Guid.NewGuid().ToString();
			Name = name;
			Parent = parent;
			Type = type;
		}

		#region Serialization

		public record SerializedConnection(string Id, string Name, string TypeInfo, string SerializedType, List<string> Connections);
		internal string Serialize()
		{
			var connections = Connections.ToList();

			var serializedConnection = new SerializedConnection(Id, Name, Type.GetType().FullName!, Type.Serialize(), Connections.Select(x => x.Id).ToList());

			return JsonSerializer.Serialize(serializedConnection);
		}

		internal static Connection Deserialize(Node parent, string serializedConnection, out SerializedConnection serializedConnectionObj)
		{
			serializedConnectionObj = JsonSerializer.Deserialize<SerializedConnection>(serializedConnection) ?? throw new Exception($"Unable to deserialize connection");
			var type = TypeBase.Deserialize(serializedConnectionObj.TypeInfo, serializedConnectionObj.SerializedType);
			var connection = new Connection(serializedConnectionObj.Name, parent, type, serializedConnectionObj.Id);

			return connection;
		}

		internal void DeserializeConnectionLinks(Graph graph, SerializedConnection serializedConnection)
		{
			foreach (var connectionId in serializedConnection.Connections)
			{
				var otherConnection = graph.Nodes.SelectMany(x => x.Value.InputsAndOutputs).FirstOrDefault(x => x.Id == connectionId);
				if (otherConnection == null)
					throw new Exception("Connection not found:" + connectionId);

				Connections.Add(otherConnection);
			}
		}


		#endregion
	}
}
