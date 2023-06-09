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


		internal Class.IMemberInfo? TargetMember;


		public SetPropertyOrField(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "Get";
		}

		protected override void Deserialize(SerializedNode serializedNodeObj)
		{
			base.Deserialize(serializedNodeObj);

			if (Decorations.TryGetValue(typeof(GetPropertyOrFieldDecoration), out var decoration))
			{
				TargetMember = ((GetPropertyOrFieldDecoration)decoration).TargetPropertyOrField;
				Name = TargetMember.DeclaringType.FriendlyName + "." + TargetMember.Name;
			}
		}

		internal void SetMemberTarget(Class.IMemberInfo memberInfo)
		{
			TargetMember = memberInfo;
			Decorations[typeof(GetPropertyOrFieldDecoration)] = new GetPropertyOrFieldDecoration(TargetMember);

			Name = TargetMember.DeclaringType.FriendlyName + "." + TargetMember.Name;

			bool isStatic = TargetMember.IsStatic;

			if (!isStatic)
				Inputs.Add(new("Target", this, TargetMember.DeclaringType));

			Inputs.Add(new Connection("Value", this, TargetMember.MemberType));
			Outputs.Add(new Connection("Value", this, TargetMember.MemberType));
		}


		protected override void ExecuteInternal(object? self, object?[] inputs, object?[] outputs)
		{
			if(self == null)
				throw new ArgumentNullException(nameof(self));

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
						field.SetValue(inputs[1], value = inputs[2]);

					outputs[1] = value;

					return;
				}
				else if (realMemberInfo.MemberInfo.MemberType == MemberTypes.Property)
				{
					var property = (PropertyInfo)TargetMember;
					object? value;
					if (property.GetMethod!.IsStatic)
						property.SetValue(null, value = inputs[1]);
					else
						property.SetValue(inputs[1], value = inputs[2]);
					outputs[1] = value;
					return;
				}
			}
			else if(TargetMember is NodeClassPropertyMemberInfo)
			{
				var property = self.GetType().GetProperty(TargetMember.Name, BindingFlags.Public | BindingFlags.Instance) ?? throw new Exception("unable to get property: " + TargetMember.Name);
				property.SetValue(self, inputs[1]);
				outputs[1] = inputs[1];
			}
		}

	}
}
