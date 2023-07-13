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
	public class GetPropertyOrField : NoFlowNode
	{
		public class RealMemberInfo : Class.IMemberInfo
		{
			internal readonly MemberInfo MemberInfo;
			private readonly TypeFactory TypeFactory;

			public RealMemberInfo(MemberInfo memberInfo, TypeFactory typeFactory)
			{
				MemberInfo = memberInfo;
				TypeFactory = typeFactory;
			}

			public TypeBase DeclaringType => TypeFactory.Get(MemberInfo.DeclaringType!);

			public string Name => MemberInfo.Name;

			public TypeBase MemberType => MemberInfo switch
			{
				FieldInfo field => TypeFactory.Get(field.FieldType),
				PropertyInfo property => TypeFactory.Get(property.PropertyType),
				_ => throw new Exception("Invalid member type")
			};

			public bool IsStatic => MemberInfo switch
			{
				FieldInfo field => field.IsStatic,
				PropertyInfo property => property.GetMethod?.IsStatic ?? false,
				_ => throw new Exception("Invalid member type")
			};
		}

		public class GetPropertyOrFieldDecoration : INodeDecoration
		{
			private record class SavedGetPropertyOrField(string Type, string SerializedType, string Name);
			internal Class.IMemberInfo TargetPropertyOrField { get; set; }

			internal GetPropertyOrFieldDecoration(Class.IMemberInfo targetPropertyOrField)
			{
				TargetPropertyOrField = targetPropertyOrField;
			}

			public string Serialize()
			{
				return JsonSerializer.Serialize(new SavedGetPropertyOrField(TargetPropertyOrField.DeclaringType.GetType().FullName!, TargetPropertyOrField.DeclaringType.FullName, TargetPropertyOrField.Name));
			}

			public static INodeDecoration Deserialize(TypeFactory typeFactory, string Json)
			{
				var info = JsonSerializer.Deserialize<SavedGetPropertyOrField>(Json) ?? throw new Exception("Unable to deserialize property or field info");

				var type = TypeBase.Deserialize(typeFactory, info.Type, info.SerializedType);

				if (type is RealType realType)
				{
					var member = realType.BackendType.GetMember(info.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetField | BindingFlags.GetProperty).FirstOrDefault() ?? throw new Exception("Unable to find member: " + info.Name);
					return new GetPropertyOrFieldDecoration(new RealMemberInfo(member, typeFactory));
				}
				else if (type is NodeClassType nodeClassType)
				{
					var property = nodeClassType.NodeClass.Properties.FirstOrDefault(x => x.Name == info.Name) ?? throw new Exception("Unable to find property: " + info.Name);
					return new GetPropertyOrFieldDecoration(property);
				}
				else
					throw new Exception("Unknown type in GetPropertyOrFieldDecoration: " + type.Name);
			}
		}

		public override string TitleColor => "lightblue";


		internal Class.IMemberInfo? TargetMember;

		public override string Name
		{
			get => TargetMember == null ? "Get" : "Get " + TargetMember.DeclaringType.FriendlyName + "." + TargetMember.Name;
			set { }
		}

		public GetPropertyOrField(Graph graph, string? id = null) : base(graph, id)
		{
		}

		protected override void Deserialize(SerializedNode serializedNodeObj)
		{
			base.Deserialize(serializedNodeObj);

			if (Decorations.TryGetValue(typeof(GetPropertyOrFieldDecoration), out var decoration))
				TargetMember = ((GetPropertyOrFieldDecoration)decoration).TargetPropertyOrField;
		}

		public void SetMemberTarget(Class.IMemberInfo memberInfo)
		{
			TargetMember = memberInfo;
			Decorations[typeof(GetPropertyOrFieldDecoration)] = new GetPropertyOrFieldDecoration(TargetMember);

			bool isStatic = TargetMember.IsStatic;

			if (!isStatic)
				Inputs.Add(new("Target", this, TargetMember.DeclaringType));

			Outputs.Add(new Connection("Value", this, TargetMember.MemberType));
		}


		protected override void ExecuteInternal(GraphExecutor graphExecutor, object? self, Span<object?> inputs, Span<object?> outputs)
		{
			if (TargetMember == null)
				throw new Exception("Target method is not set");

			if (TargetMember is RealMemberInfo realMemberInfo)
			{
				if (realMemberInfo.MemberInfo.MemberType == MemberTypes.Field)
				{
					var field = (FieldInfo)realMemberInfo.MemberInfo;
					object? target = field.IsStatic ? null : inputs[0];
					var result = field.GetValue(target);
					outputs[0] = result;
					return;
				}
				else if (realMemberInfo.MemberInfo.MemberType == MemberTypes.Property)
				{
					var property = (PropertyInfo)realMemberInfo.MemberInfo;
					object? target = property.GetMethod!.IsStatic ? null : inputs[0];
					var result = property.GetValue(target);
					outputs[0] = result;
					return;
				}
			}
			else
			{
				Type t;
				if (Inputs[0].Type is RealType r)
					t = r.BackendType;
				else
					t = Project.GetCreatedClassType(((NodeClassType)Inputs[0].Type).NodeClass);

				var property = t.GetProperty(TargetMember.Name, BindingFlags.Public | BindingFlags.Instance) ?? throw new Exception("unable to get property: " + TargetMember.Name);
				var result = property.GetValue(inputs[0] ?? self);
				outputs[0] = result;
			}
		}
	}
}
