using NodeDev.Core.Class;
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

		var assign = Expression.Assign(member, info.LocalVariables[Inputs[1]]);
		var assignOutput = Expression.Assign(info.LocalVariables[Outputs[0]], assign); // "output = (field = value)". This allows returning one expression for both assignations

		return assignOutput;
	}

	protected override void ExecuteInternal(GraphExecutor executor, object? self, Span<object?> inputs, Span<object?> outputs, ref object? state)
	{
		if (TargetMember == null)
			throw new Exception("Target method is not set");

		if (TargetMember is RealMemberInfo realMemberInfo)
		{
			if (realMemberInfo.MemberInfo.MemberType == MemberTypes.Field)
			{
				var field = (FieldInfo)TargetMember;
				object? value;
				if (field.IsStatic)
					field.SetValue(null, value = inputs[1]);
				else
					field.SetValue(inputs[0] ?? self, value = inputs[2]);

				outputs[1] = value;

				return;
			}
			else if (realMemberInfo.MemberInfo.MemberType == MemberTypes.Property)
			{
				var property = (PropertyInfo)TargetMember;
				object? value;
				if (property.SetMethod!.IsStatic)
					property.SetValue(null, value = inputs[1]);
				else
					property.SetValue(inputs[0] ?? self, value = inputs[2]);
				outputs[1] = value;
				return;
			}
		}
		else if (TargetMember is NodeClassProperty)
		{
			Type t;
			if (Inputs[0].Type is RealType r)
				t = r.BackendType;
			else
				t = Project.GetCreatedClassType(((NodeClassType)Inputs[0].Type).NodeClass);

			var property = t.GetProperty(TargetMember.Name, BindingFlags.Public | BindingFlags.Instance) ?? throw new Exception("unable to get property: " + TargetMember.Name);
			object? value;
			if (property.SetMethod!.IsStatic)
				property.SetValue(null, value = inputs[1]);
			else
				property.SetValue(inputs[0] ?? self, value = inputs[2]);
			outputs[1] = value;
		}
	}

}
