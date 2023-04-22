using NodeDev.Core.NodeDecorations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static MudBlazor.Colors;

namespace NodeDev.Blazor.NodeAttributes
{
	public class NodeDecorationPosition : NodeDecoration
	{
		public NodeDecorationPosition(Vector2 position)
		{
			Position = position;
		}

		public Vector2 Position { get; set; }

		public float X => Position.X;
		public float Y => Position.Y;


		private record class SerializedNodeDecoration(float X, float Y);
		public override string Serialize()
		{
			return JsonSerializer.Serialize(new SerializedNodeDecoration(X, Y));
		}

		public static NodeDecorationPosition Deserialize(string Json)
		{
			var serializedNodeDecoration = JsonSerializer.Deserialize<SerializedNodeDecoration>(Json) ?? throw new Exception("Unable to deserialize node decoration");

			return new NodeDecorationPosition(new(serializedNodeDecoration.X, serializedNodeDecoration.Y));
		}
	}
}
