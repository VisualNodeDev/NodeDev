using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes;

public class ArraySet : NormalFlowNode
{
	public override string Name
	{
		get => $"{Inputs[0].Type.Name} Set";
		set { }
	}

	public ArraySet(Graph graph, string? id = null) : base(graph, id)
	{
		var undefinedT = new UndefinedGenericType("T");

		Inputs.Add(new("Array", this, undefinedT.ArrayType));
		Inputs.Add(new("Index", this, TypeFactory.Get<int>()));
		Inputs.Add(new("Obj", this, undefinedT));
	}

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		if (!Inputs[1].Type.IsArray)
			throw new Exception("ArrayGet.Inputs[1] should be an array type");

		var arrayIndex = Expression.ArrayIndex(info.LocalVariables[Inputs[1]], info.LocalVariables[Inputs[2]]);
		return Expression.Assign(arrayIndex, info.LocalVariables[Inputs[3]]);
	}
}
