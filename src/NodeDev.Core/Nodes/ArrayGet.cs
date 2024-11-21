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

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		if (!Inputs[0].Type.IsArray)
			throw new Exception("ArrayGet.Inputs[0] should be an array type");

		var arrayIndex = Expression.ArrayIndex(info.LocalVariables[Inputs[0]], info.LocalVariables[Inputs[1]]);
		info.LocalVariables[Outputs[0]] = arrayIndex;
	}
}
