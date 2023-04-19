using NodeDev.Core.NodeDecorations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
	}
}
