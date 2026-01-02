using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;
using NodeDev.Core.Connections;
using NodeDev.Core.NodeDecorations;
using NodeDev.Core.Types;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.Nodes;

public class GetPropertyOrField : NoFlowNode
{
	public class GetPropertyOrFieldDecoration : INodeDecoration
	{
		private record class SavedGetPropertyOrField(string Type, string Name);
		internal IMemberInfo TargetPropertyOrField { get; set; }

		internal GetPropertyOrFieldDecoration(IMemberInfo targetPropertyOrField)
		{
			TargetPropertyOrField = targetPropertyOrField;
		}

		public string Serialize()
		{
			return JsonSerializer.Serialize(new SavedGetPropertyOrField(TargetPropertyOrField.DeclaringType.SerializeWithFullTypeNameString(), TargetPropertyOrField.Name));
		}

		public static INodeDecoration Deserialize(TypeFactory typeFactory, string Json)
		{
			var info = JsonSerializer.Deserialize<SavedGetPropertyOrField>(Json) ?? throw new Exception("Unable to deserialize property or field info");

			var type = TypeBase.DeserializeFullTypeNameString(typeFactory, info.Type);

			if (type is RealType realType)
			{
				var member = realType.BackendType.GetMember(info.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.GetField | BindingFlags.GetProperty).FirstOrDefault() ?? throw new Exception("Unable to find member: " + info.Name);
				return new GetPropertyOrFieldDecoration(new RealMemberInfo(member, realType));
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


	internal IMemberInfo? TargetMember;

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

	public void SetMemberTarget(IMemberInfo memberInfo)
	{
		TargetMember = memberInfo;
		Decorations[typeof(GetPropertyOrFieldDecoration)] = new GetPropertyOrFieldDecoration(TargetMember);

		bool isStatic = TargetMember.IsStatic;

		if (!isStatic)
			Inputs.Add(new("Target", this, TargetMember.DeclaringType));

		Outputs.Add(new Connection("Value", this, TargetMember.MemberType));
	}

	internal override void BuildInlineExpression(BuildExpressionInfo info)
	{
		if (TargetMember == null)
			throw new InvalidOperationException("Target member is not set");

		var type = TargetMember.DeclaringType.MakeRealType();

		var binding = BindingFlags.Public | BindingFlags.NonPublic | (TargetMember.IsStatic ? BindingFlags.Static : BindingFlags.Instance);

		if (TargetMember.IsField)
		{
			var field = type.GetField(TargetMember.Name, binding | BindingFlags.GetField) ?? throw new Exception($"Unable to find field: {TargetMember.Name}");

			info.LocalVariables[Outputs[0]] = Expression.Field(TargetMember.IsStatic ? null : info.LocalVariables[Inputs[0]], field);
		}
		else
		{
			var property = type.GetProperty(TargetMember.Name, binding | BindingFlags.GetProperty) ?? throw new Exception($"Unable to find property: {TargetMember.Name}");

			info.LocalVariables[Outputs[0]] = Expression.Property(TargetMember.IsStatic ? null : info.LocalVariables[Inputs[0]], property);
		}
	}

	internal override ExpressionSyntax GenerateRoslynExpression(GenerationContext context)
	{
		if (TargetMember == null)
			throw new InvalidOperationException("Target member is not set");

		// Build the member access expression
		if (TargetMember.IsStatic)
		{
			// Static: ClassName.MemberName
			var typeSyntax = RoslynHelpers.GetTypeSyntax(TargetMember.DeclaringType);
			return SF.MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				typeSyntax,
				SF.IdentifierName(TargetMember.Name));
		}
		else
		{
			// Instance: target.MemberName
			var targetVar = SF.IdentifierName(context.GetVariableName(Inputs[0])!);
			return SF.MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				targetVar,
				SF.IdentifierName(TargetMember.Name));
		}
	}
}
