using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.NodeDecorations
{
	internal class NodeDecorationString : NodeDecoration
	{
		public string Value { get; }

		public NodeDecorationString(string value)
		{
			Value = value;
		}
	}
}
