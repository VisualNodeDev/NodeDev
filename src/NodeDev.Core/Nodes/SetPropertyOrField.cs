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

namespace NodeDev.Core.Nodes
{
	public class SetPropertyOrField : NormalFlowNode
	{
		public class SetPropertyOrFieldDecoration : INodeDecoration
		{
			private record class SavedSetPropertyOrField(string Type, string Name);
			internal MemberInfo TargetPropertyOrField { get; set; }

			public SetPropertyOrFieldDecoration(MemberInfo targetPropertyOrField)
			{
				TargetPropertyOrField = targetPropertyOrField;
			}

			public string Serialize()
			{
				return JsonSerializer.Serialize(new SavedSetPropertyOrField(TargetPropertyOrField.DeclaringType!.FullName!, TargetPropertyOrField.Name));
			}

			public static INodeDecoration Deserialize(string Json)
			{
				var info = JsonSerializer.Deserialize<SavedSetPropertyOrField>(Json) ?? throw new Exception("Unable to deserialize property or field info");
				var type = Type.GetType(info.Type) ?? throw new Exception("Unable to find type: " + info.Type);
				var member = type.GetMember(info.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetField | BindingFlags.GetProperty).FirstOrDefault() ?? throw new Exception("Unable to find member: " + info.Name);

				return new SetPropertyOrFieldDecoration(member);
			}
		}

		public override string TitleColor => "lightblue";


		internal MemberInfo? TargetMember;


		public SetPropertyOrField(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "Get";
		}

		protected override void Deserialize(SerializedNode serializedNodeObj)
		{
			base.Deserialize(serializedNodeObj);

			if (Decorations.TryGetValue(typeof(SetPropertyOrFieldDecoration), out var decoration))
			{
				TargetMember = ((SetPropertyOrFieldDecoration)decoration).TargetPropertyOrField;
				Name = TypeFactory.Get(TargetMember.DeclaringType!).FriendlyName + "." + TargetMember.Name;
			}
		}

		internal void SetMemberTarget(MemberInfo memberInfo)
		{
			TargetMember = memberInfo;
			Decorations[typeof(SetPropertyOrFieldDecoration)] = new SetPropertyOrFieldDecoration(TargetMember);

			Name = TypeFactory.Get(TargetMember.DeclaringType!).FriendlyName + "." + TargetMember.Name;

			bool isStatic = TargetMember switch
			{
				FieldInfo field => field.IsStatic,
				PropertyInfo property => property.GetMethod?.IsStatic ?? false,
				_ => throw new Exception("Invalid member type")
			};
			if (!isStatic)
				Inputs.Add(new("Target", this, TypeFactory.Get(TargetMember.DeclaringType!)));

			var type = TargetMember switch
			{
				FieldInfo field => field.FieldType,
				PropertyInfo property => property.PropertyType,
				_ => throw new Exception("Invalid member type")
			};
			Inputs.Add(new Connection("Value", this, TypeFactory.Get(type)));
			Outputs.Add(new Connection("Value", this, TypeFactory.Get(type)));
		}


		protected override void ExecuteInternal(object? self, object?[] inputs, object?[] outputs)
		{
			if (TargetMember == null)
				throw new Exception("Target method is not set");

			if (TargetMember.MemberType == MemberTypes.Field)
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
			else if(TargetMember.MemberType == MemberTypes.Property)
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

	}
}
