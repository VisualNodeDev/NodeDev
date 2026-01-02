using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.CodeGeneration;
using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;
using System.Reflection;
using static NodeDev.Core.Nodes.GetPropertyOrField;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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

	internal override StatementSyntax GenerateRoslynStatement(Dictionary<Connection, Graph.NodePathChunks>? subChunks, GenerationContext context)
	{
		if (TargetMember == null)
			throw new Exception($"Target member is not set in SetPropertyOrField {Name}");

		// Build the member access expression
		ExpressionSyntax memberAccess;
		if (TargetMember.IsStatic)
		{
			// Static: ClassName.MemberName
			var typeSyntax = RoslynHelpers.GetTypeSyntax(TargetMember.DeclaringType);
			memberAccess = SF.MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				typeSyntax,
				SF.IdentifierName(TargetMember.Name));
		}
		else
		{
			// Instance: target.MemberName
			var targetVar = SF.IdentifierName(context.GetVariableName(Inputs[0])!);
			memberAccess = SF.MemberAccessExpression(
				SyntaxKind.SimpleMemberAccessExpression,
				targetVar,
				SF.IdentifierName(TargetMember.Name));
		}

		var valueVar = SF.IdentifierName(context.GetVariableName(Inputs[^1])!);
		var outputVar = SF.IdentifierName(context.GetVariableName(Outputs[1])!);

		// Generate: output = (member = value)
		var innerAssignment = SF.AssignmentExpression(
			SyntaxKind.SimpleAssignmentExpression,
			memberAccess,
			valueVar);

		var outerAssignment = SF.AssignmentExpression(
			SyntaxKind.SimpleAssignmentExpression,
			outputVar,
			SF.ParenthesizedExpression(innerAssignment));

		return SF.ExpressionStatement(outerAssignment);
	}
}
