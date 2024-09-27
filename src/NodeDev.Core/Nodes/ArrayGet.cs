using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes;

public class ArrayGet : NoFlowNode
{
	public override string Name
	{
		get => $"{Outputs[0].Type.Name} Get";
        set { }
	}

	public ArrayGet(Graph graph, string? id = null) : base(graph, id)
	{
		var undefinedT = new UndefinedGenericType("T");

        Inputs.Add(new("Array", this, undefinedT.ArrayType));
		Inputs.Add(new("Index", this, TypeFactory.Get<int>()));

		Outputs.Add(new("Obj", this, undefinedT));
	}

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		if (!Inputs[0].Type.IsArray)
			throw new Exception("ArrayGet.Inputs[0] should be an array type");

		var arrayIndex = Expression.ArrayIndex(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
        return Expression.Assign(info.LocalVariables[Outputs[1]], arrayIndex);
	}
}
