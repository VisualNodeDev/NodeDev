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
	public class GetPropertyOrField : NoFlowNode
	{
		public class GetPropertyOrFieldDecoration : INodeDecoration
		{
			private record class SavedGetPropertyOrField(string Type, string Name);
			internal MemberInfo TargetPropertyOrField { get; set; }

			public GetPropertyOrFieldDecoration(MemberInfo targetPropertyOrField)
			{
				TargetPropertyOrField = targetPropertyOrField;
			}

			public string Serialize()
			{
				return JsonSerializer.Serialize(new SavedGetPropertyOrField(TargetPropertyOrField.DeclaringType!.FullName!, TargetPropertyOrField.Name));
			}

			public static INodeDecoration Deserialize(string Json)
			{
				var info = JsonSerializer.Deserialize<SavedGetPropertyOrField>(Json) ?? throw new Exception("Unable to deserialize property or field info");
				var type = Type.GetType(info.Type) ?? throw new Exception("Unable to find type: " + info.Type);
				var member = type.GetMember(info.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetField | BindingFlags.GetProperty).FirstOrDefault() ?? throw new Exception("Unable to find member: " + info.Name);

				return new GetPropertyOrFieldDecoration(member);
			}
		}

		public override string TitleColor => "lightblue";


		internal MemberInfo? TargetMember;


		public GetPropertyOrField(Graph graph, string? id = null) : base(graph, id)
		{
			Name = "Get";
		}

		protected override void Deserialize(SerializedNode serializedNodeObj)
		{
			base.Deserialize(serializedNodeObj);

			if (Decorations.TryGetValue(typeof(GetPropertyOrFieldDecoration), out var decoration))
			{
				TargetMember = ((GetPropertyOrFieldDecoration)decoration).TargetPropertyOrField;
				Name = TypeFactory.Get(TargetMember.DeclaringType!).FriendlyName + "." + TargetMember.Name;
			}
		}

		internal void SetMemberTarget(MemberInfo memberInfo)
		{
			TargetMember = memberInfo;
			Decorations[typeof(GetPropertyOrFieldDecoration)] = new GetPropertyOrFieldDecoration(TargetMember);

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
			Outputs.Add(new Connection("Value", this, TypeFactory.Get(type)));
		}


		protected override void ExecuteInternal(object?[] inputs, object?[] outputs)
		{
			if (TargetMember == null)
				throw new Exception("Target method is not set");

			if (TargetMember.MemberType == MemberTypes.Field)
			{
				var field = (FieldInfo)TargetMember;
				object? target = field.IsStatic ? null : inputs[0];
				var result = field.GetValue(target);
				outputs[1] = result;
				return;
			}
			else if(TargetMember.MemberType == MemberTypes.Property)
			{
				var property = (PropertyInfo)TargetMember;
				object? target = property.GetMethod!.IsStatic ? null : inputs[0];
				var result = property.GetValue(target);
				outputs[1] = result;
				return;
			}
		}

	}
}
