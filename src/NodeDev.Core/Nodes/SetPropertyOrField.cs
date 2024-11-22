using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;
using System.Reflection;
using static NodeDev.Core.Nodes.GetPropertyOrField;

namespace NodeDev.Core.Nodes;

public class SetPropertyOrField : NormalFlowNode
{
	public override string TitleColor => "lightblue";


	internal IMemberInfo? TargetMember;

	public override string Name
	{
		get => TargetMember == null ? "Set" : "Set " + TargetMember.DeclaringType.FriendlyName + "." + TargetMember.Name;
		set { }
	}

	public SetPropertyOrField(Graph graph, string? id = null) : base(graph, id)
	{
	}

	protected override void Deserialize(SerializedNode serializedNodeObj)
	{
		base.Deserialize(serializedNodeObj);

		if (Decorations.TryGetValue(typeof(GetPropertyOrFieldDecoration), out var decoration))
		{
			TargetMember = ((GetPropertyOrFieldDecoration)decoration).TargetPropertyOrField;
		}
	}

	public void SetMemberTarget(IMemberInfo memberInfo)
	{
		TargetMember = memberInfo;
		Decorations[typeof(GetPropertyOrFieldDecoration)] = new GetPropertyOrFieldDecoration(TargetMember);

		bool isStatic = TargetMember.IsStatic;

		if (!isStatic)
			Inputs.Insert(0, new("Target", this, TargetMember.DeclaringType));

		Inputs.Add(new Connection("Value", this, TargetMember.MemberType));
		Outputs.Add(new Connection("Value", this, TargetMember.MemberType));
	}

	internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
	{
		if (TargetMember == null)
			throw new Exception($"Target member is not set in SetPropertyOrField {Name}");

		var type = TargetMember.DeclaringType.MakeRealType();

		var binding = BindingFlags.Public | BindingFlags.NonPublic | (TargetMember.IsStatic ? BindingFlags.Static : BindingFlags.Instance);

		MemberExpression member;
		if (TargetMember.IsField)
		{
			var field = type.GetField(TargetMember.Name, binding | BindingFlags.SetField) ?? throw new Exception($"Unable to find field: {TargetMember.Name}");
			member = Expression.Field(TargetMember.IsStatic ? null : info.LocalVariables[Inputs[0]], field);
		}
		else
		{
			var property = type.GetProperty(TargetMember.Name, binding | BindingFlags.SetProperty) ?? throw new Exception($"Unable to find property: {TargetMember.Name}");
			member = Expression.Property(TargetMember.IsStatic ? null : info.LocalVariables[Inputs[0]], property);
		}

		var assign = Expression.Assign(member, info.LocalVariables[Inputs[^1]]);
		var assignOutput = Expression.Assign(info.LocalVariables[Outputs[1]], assign); // "output = (field = value)". This allows returning one expression for both assignations

		return assignOutput;
	}
}
