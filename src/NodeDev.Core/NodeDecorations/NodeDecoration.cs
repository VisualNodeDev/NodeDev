using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.NodeDecorations;

public class NodeDecoration
{
	public NodeDecoration(string name)
	{
		Name = name;
	}

	public NodeDecoration()
	{
		Name = GetType().Name;
	}

	public string Name { get; }
}
