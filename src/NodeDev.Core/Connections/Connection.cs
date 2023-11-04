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

		public TypeBase Type { get; private set; }

		public ICollection<Connection> Connections { get; } = new List<Connection>();

		public string? TextboxValue { get; private set; }
		public object? ParsedTextboxValue { get; set; }

		public Connection(string name, Node parent, TypeBase type, string? id = null)
		{
			Id = id ?? Guid.NewGuid().ToString();
			Name = name;
			Parent = parent;
			Type = type;
		}

		#region Serialization

		public record SerializedConnection(string Id, string Name, string SerializedType, List<string> Connections, string? TextboxValue);
		internal string Serialize()
		{
			var connections = Connections.ToList();

			var serializedConnection = new SerializedConnection(Id, Name, Type.SerializeWithFullTypeName(), Connections.Select(x => x.Id).ToList(), TextboxValue);

			return JsonSerializer.Serialize(serializedConnection);
		}

		internal static Connection Deserialize(Node parent, string serializedConnection)
		{
			var serializedConnectionObj = JsonSerializer.Deserialize<SerializedConnection>(serializedConnection) ?? throw new Exception($"Unable to deserialize connection");
			var type = TypeBase.Deserialize(parent.TypeFactory, serializedConnectionObj.SerializedType);
			var connection = new Connection(serializedConnectionObj.Name, parent, type, serializedConnectionObj.Id);

			connection.TextboxValue = serializedConnectionObj.TextboxValue;
			if (connection.TextboxValue != null)
				connection.ParsedTextboxValue = connection.Type.ParseTextboxEdit(connection.TextboxValue);

			foreach (var connectionId in serializedConnectionObj.Connections)
			{
				var otherConnection = parent.Graph.Nodes.SelectMany(x => x.Value.InputsAndOutputs).FirstOrDefault(x => x.Id == connectionId);
				if (otherConnection == null)
					continue;

				if(!connection.Connections.Contains(otherConnection))
                    connection.Connections.Add(otherConnection);
				if(!otherConnection.Connections.Contains(connection))
                    otherConnection.Connections.Add(connection);
			}

			return connection;
		}

		#endregion

        public void UpdateType(TypeBase newType)
        {
			Type = newType;

			if (Type.AllowTextboxEdit)
				TextboxValue = Type.DefaultTextboxValue;
			else
				TextboxValue = null;
        }

		public void UpdateTextboxText(string? text)
        {
			if (Type.AllowTextboxEdit)
			{
				TextboxValue = text;
				if (text == null)
					ParsedTextboxValue = null;
				else
				{
					try
					{
						ParsedTextboxValue = Type.ParseTextboxEdit(text);
					}
					catch (Exception)
					{ }
				}
			}
        }


    }
}
