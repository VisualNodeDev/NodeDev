using NodeDev.Core.Nodes;
using NodeDev.Core.Types;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace NodeDev.Core.Connections
{
	[System.Diagnostics.DebuggerDisplay("{Parent.Name}:{Name} - {Type}, {Connections.Count}")]
	public class Connection
	{
		public string Id { get; }

		public string Name { get; set; }

		public Node Parent { get; }

		public TypeBase Type { get; private set; }

		/// <summary>
		/// Initial type of the connection. Used to remember the type before generics were resolved in case we need to re-resolve them differently.
		/// </summary>
		public TypeBase InitialType { get; private set; }

		internal readonly List<Connection> _Connections = [];

		public IReadOnlyList<Connection> Connections => _Connections;

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

		public bool IsInput => Parent.Inputs.Contains(this);
		public bool IsOutput => Parent.Outputs.Contains(this);

		public Connection(string name, Node parent, TypeBase type, string? id = null, Connection? linkedExec = null)
		{
			Id = id ?? Guid.NewGuid().ToString();
			Name = name;
			Parent = parent;
			Type = type;
			InitialType = type;
			LinkedExec = linkedExec;
		}

		#region Serialization

		public record class SerializedConnectionVertex(float X, float Y);
		public record SerializedConnection(string Id, string Name, TypeBase.SerializedType SerializedType, List<string> Connections, string? TextboxValue, List<SerializedConnectionVertex>? Vertices, string? LinkedExec);
		internal SerializedConnection Serialize()
		{
			var connections = Connections.ToList();

			var serializedConnection = new SerializedConnection(Id, Name, Type.SerializeWithFullTypeName(), Connections.Select(x => x.Id).ToList(), TextboxValue, Vertices.Select(x => new SerializedConnectionVertex(x.X, x.Y)).ToList(), LinkedExec?.Id);

			return serializedConnection;
		}

		internal static Connection Deserialize(Node parent, SerializedConnection serializedConnectionObj, bool isInput)
		{
			// Find the LinkedExec connection, if any
			Connection? linkedExec = null;
			if (linkedExec != null)
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
					connection._Connections.Add(otherConnection);
				if (!otherConnection.Connections.Contains(connection))
					otherConnection._Connections.Add(connection);
			}

			return connection;
		}

		#endregion

		public bool IsAssignableTo(Connection other, bool alsoValidateInitialTypeSource, bool alsoValidateInitialTypeDestination, [MaybeNullWhen(false)] out Dictionary<string, TypeBase> changedGenericsLeft, [MaybeNullWhen(false)] out Dictionary<string, TypeBase> changedGenericsRight, out bool usedInitialTypes)
		{
			if (Type.IsAssignableTo(other.Type, out var changedGenericsLeft1, out var changedGenericsRight1, out var depth1))
			{
				if (alsoValidateInitialTypeSource || alsoValidateInitialTypeDestination)
				{
					var initialType = alsoValidateInitialTypeSource ? InitialType : Type;
					var otherInitialType = alsoValidateInitialTypeDestination ? other.InitialType : other.Type;
					if ((initialType != Type || otherInitialType != other.Type) && initialType.IsAssignableTo(otherInitialType, out var changedGenericsLeft2, out var changedGenericsRight2, out var depth2))
					{
						if ((changedGenericsLeft2.Count != 0 || changedGenericsRight2.Count != 0) && depth2 < depth1)
						{
							changedGenericsLeft = changedGenericsLeft2;
							changedGenericsRight = changedGenericsRight2;
							usedInitialTypes = true;
							return true;
						}
					}
				}

				changedGenericsLeft = changedGenericsLeft1;
				changedGenericsRight = changedGenericsRight1;
				usedInitialTypes = false;
				return true;
			}

			changedGenericsLeft = changedGenericsRight = null;
			usedInitialTypes = false;
			return false;
		}

		public void UpdateVertices(IEnumerable<Vector2> vertices)
		{
			Vertices.Clear();
			Vertices.AddRange(vertices);
		}


		public void UpdateTypeAndTextboxVisibility(TypeBase newType, bool overrideInitialType)
		{
			Type = newType;

			if (overrideInitialType)
				InitialType = newType;

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
