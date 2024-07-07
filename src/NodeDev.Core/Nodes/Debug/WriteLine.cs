using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes.Debug;

public class WriteLine : NormalFlowNode
{
	public WriteLine(Graph graph, string? id = null) : base(graph, id)
	{
		Name = "WriteLine";

		Inputs.Add(new("Line", this, new UndefinedGenericType("T")));
	}

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		if (subChunks != null)
			throw new Exception("WriteLine node should not have subchunks");

		var method = typeof(Console).GetMethod(nameof(Console.WriteLine), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, [typeof(object)]);
		if (method == null)
			throw new Exception("Unable to find Console.WriteLine method");

		return Expression.Call(null, method, info.LocalVariables[Inputs[1]]);
	}
}
