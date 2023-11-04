using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.NodeDecorations;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static NodeDev.Core.Nodes.GetPropertyOrField;

namespace NodeDev.Core.Nodes
{
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


		protected override void ExecuteInternal(GraphExecutor executor, object? self, Span<object?> inputs, Span<object?> outputs)
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
			else if(TargetMember is NodeClassProperty)
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
}
