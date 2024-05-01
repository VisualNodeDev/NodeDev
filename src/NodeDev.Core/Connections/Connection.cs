using NodeDev.Core.Nodes;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NodeDev.Core.Connections
{
	[System.Diagnostics.DebuggerDisplay("{Parent.Name}:{Name} - {Type}, {Connections.Count}")]
	public class Connection
	{
		public string Id { get; }

		public string Name { get; set; }

		public Node Parent { get; }

		public TypeBase Type { get; private set; }

		public readonly ICollection<Connection> Connections = [];

		/// <summary>
		/// Vertices of the connection. Used for drawing connections with multiple segments.
		/// This is defined either if the connection is an input AND not exec, or if the connection is an output AND exec.
		/// This is because for every possible types of inputs there is always only one output connected to it. Except for execs since multiple output paths can be connected to it.
		/// </summary>
		public readonly List<Vector2> Vertices = [];

		public string? TextboxValue { get; private set; }

		public object? ParsedTextboxValue { get; set; }

		/// <summary>
		/// Global index of this connection in the graph. Each connection of each node in a graph has a unique index.
		/// </summary>
		public int GraphIndex { get; set; } = -1;

		/// <summary>
		/// LinkedExec can be used to make sure a connection can only ever be used while on a path inside the linked exec connection.
		/// Ex. during a 'foreach' loop, the 'Item' connection can only be used inside the 'Loop Exec' connection.
		/// </summary>
		public Connection? LinkedExec { get; }

		public Connection(string name, Node parent, TypeBase type, string? id = null, Connection? linkedExec = null)
		{
			Id = id ?? Guid.NewGuid().ToString();
			Name = name;
			Parent = parent;
			Type = type;
			LinkedExec = linkedExec;
		}

		#region Serialization

		private record class SerializedConnectionVertex(float X, float Y);
		private record SerializedConnection(string Id, string Name, string SerializedType, List<string> Connections, string? TextboxValue, List<SerializedConnectionVertex>? Vertices, string? LinkedExec);
		internal string Serialize()
		{
			var connections = Connections.ToList();

			var serializedConnection = new SerializedConnection(Id, Name, Type.SerializeWithFullTypeName(), Connections.Select(x => x.Id).ToList(), TextboxValue, Vertices.Select(x => new SerializedConnectionVertex(x.X, x.Y)).ToList(), LinkedExec?.Id);

			return JsonSerializer.Serialize(serializedConnection);
		}

		internal static Connection Deserialize(Node parent, string serializedConnection, bool isInput)
		{
			var serializedConnectionObj = JsonSerializer.Deserialize<SerializedConnection>(serializedConnection) ?? throw new Exception($"Unable to deserialize connection");

			// Find the LinkedExec connection, if any
			Connection? linkedExec = null;
			if(linkedExec != null)
				linkedExec = parent.Graph.Nodes.SelectMany(x => x.Value.InputsAndOutputs).FirstOrDefault(x => x.Id == serializedConnectionObj.LinkedExec);

			var type = TypeBase.Deserialize(parent.TypeFactory, serializedConnectionObj.SerializedType);
			var connection = new Connection(serializedConnectionObj.Name, parent, type, serializedConnectionObj.Id, linkedExec);

			connection.TextboxValue = serializedConnectionObj.TextboxValue;
			if (connection.TextboxValue != null && isInput)
				connection.ParsedTextboxValue = connection.Type.ParseTextboxEdit(connection.TextboxValue);

			if (serializedConnectionObj.Vertices != null)
				connection.Vertices.AddRange(serializedConnectionObj.Vertices.Select(x => new Vector2(x.X, x.Y)));

			foreach (var connectionId in serializedConnectionObj.Connections)
			{
				var otherConnection = parent.Graph.Nodes.SelectMany(x => x.Value.InputsAndOutputs).FirstOrDefault(x => x.Id == connectionId);
				if (otherConnection == null)
					continue;

				if (!connection.Connections.Contains(otherConnection))
					connection.Connections.Add(otherConnection);
				if (!otherConnection.Connections.Contains(connection))
					otherConnection.Connections.Add(connection);
			}

			return connection;
		}

		#endregion

		public void UpdateVertices(IEnumerable<Vector2> vertices)
		{
			Vertices.Clear();
			Vertices.AddRange(vertices);
		}


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
