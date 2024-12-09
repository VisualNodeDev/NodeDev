using NodeDev.Core.NodeDecorations;
using NodeDev.Core.Types;
using System.Numerics;
using System.Text.Json;

namespace NodeDev.Blazor.NodeAttributes
{
	public class NodeDecorationPosition : INodeDecoration
	{
		public NodeDecorationPosition(Vector2 position)
		{
			Position = position;
		}

		public Vector2 Position { get; set; }

		public float X => Position.X;
		public float Y => Position.Y;


		private record class SerializedNodeDecoration(float X, float Y);

		public string Serialize()
		{
			return JsonSerializer.Serialize(new SerializedNodeDecoration(X, Y));
		}

		public static INodeDecoration Deserialize(TypeFactory typeFactory, string Json)
		{
			var serializedNodeDecoration = JsonSerializer.Deserialize<SerializedNodeDecoration>(Json) ?? throw new Exception("Unable to deserialize node decoration");

			return new NodeDecorationPosition(new(serializedNodeDecoration.X, serializedNodeDecoration.Y));
		}
	}
}
