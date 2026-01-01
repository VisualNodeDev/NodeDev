using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.Types;

namespace NodeDev.Core.CodeGeneration;

/// <summary>
/// Helper class for generating Roslyn syntax nodes
/// </summary>
public static class SyntaxHelper
{
	/// <summary>
	/// Creates a TypeSyntax from a TypeBase
	/// </summary>
	public static TypeSyntax GetTypeSyntax(TypeBase type)
	{
		var typeName = type.FriendlyName;
		
		// Handle array types
		if (type is NodeClassArrayType arrayType)
		{
			var elementType = GetTypeSyntax(arrayType.ElementType);
			return SyntaxFactory.ArrayType(elementType)
				.WithRankSpecifiers(
					SyntaxFactory.SingletonList(
						SyntaxFactory.ArrayRankSpecifier(
							SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
								SyntaxFactory.OmittedArraySizeExpression()))));
		}

		// Parse the type name - handles generics like "List<int>"
		return SyntaxFactory.ParseTypeName(typeName);
	}

	/// <summary>
	/// Creates a literal expression from a value
	/// </summary>
	public static ExpressionSyntax GetLiteralExpression(object? value, TypeBase type)
	{
		if (value == null)
			return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

		// Handle primitive types
		return value switch
		{
			bool b => SyntaxFactory.LiteralExpression(b ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression),
			int i => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(i)),
			long l => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(l)),
			float f => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(f)),
			double d => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(d)),
			string s => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(s)),
			char c => SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(c)),
			_ => SyntaxFactory.DefaultExpression(GetTypeSyntax(type))
		};
	}

	/// <summary>
	/// Creates a variable declaration statement with var type
	/// </summary>
	public static LocalDeclarationStatementSyntax CreateVarDeclaration(string variableName, ExpressionSyntax? initializer = null)
	{
		var declarator = SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variableName));
		
		if (initializer != null)
		{
			declarator = declarator.WithInitializer(
				SyntaxFactory.EqualsValueClause(initializer));
		}

		return SyntaxFactory.LocalDeclarationStatement(
			SyntaxFactory.VariableDeclaration(
				SyntaxFactory.IdentifierName("var"))
			.WithVariables(
				SyntaxFactory.SingletonSeparatedList(declarator)));
	}

	/// <summary>
	/// Creates an identifier name expression
	/// </summary>
	public static IdentifierNameSyntax Identifier(string name)
	{
		return SyntaxFactory.IdentifierName(name);
	}

	/// <summary>
	/// Creates an assignment expression statement: target = value;
	/// </summary>
	public static ExpressionStatementSyntax Assignment(ExpressionSyntax target, ExpressionSyntax value)
	{
		return SyntaxFactory.ExpressionStatement(
			SyntaxFactory.AssignmentExpression(
				SyntaxKind.SimpleAssignmentExpression,
				target,
				value));
	}

	/// <summary>
	/// Creates a member access expression: target.memberName
	/// </summary>
	public static MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax target, string memberName)
	{
		return SyntaxFactory.MemberAccessExpression(
			SyntaxKind.SimpleMemberAccessExpression,
			target,
			SyntaxFactory.IdentifierName(memberName));
	}

	/// <summary>
	/// Creates an invocation expression: target(args)
	/// </summary>
	public static InvocationExpressionSyntax Invocation(ExpressionSyntax target, params ExpressionSyntax[] arguments)
	{
		return SyntaxFactory.InvocationExpression(target)
			.WithArgumentList(
				SyntaxFactory.ArgumentList(
					SyntaxFactory.SeparatedList(
						arguments.Select(SyntaxFactory.Argument))));
	}

	/// <summary>
	/// Creates a binary expression: left op right
	/// </summary>
	public static BinaryExpressionSyntax BinaryExpression(SyntaxKind kind, ExpressionSyntax left, ExpressionSyntax right)
	{
		return SyntaxFactory.BinaryExpression(kind, left, right);
	}

	/// <summary>
	/// Creates a prefix unary expression: op operand
	/// </summary>
	public static PrefixUnaryExpressionSyntax PrefixUnaryExpression(SyntaxKind kind, ExpressionSyntax operand)
	{
		return SyntaxFactory.PrefixUnaryExpression(kind, operand);
	}

	/// <summary>
	/// Creates a cast expression: (type)expression
	/// </summary>
	public static CastExpressionSyntax Cast(TypeSyntax type, ExpressionSyntax expression)
	{
		return SyntaxFactory.CastExpression(type, expression);
	}

	/// <summary>
	/// Creates a default expression: default(T) or default
	/// </summary>
	public static ExpressionSyntax Default(TypeSyntax? type = null)
	{
		if (type == null)
			return SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression);
		
		return SyntaxFactory.DefaultExpression(type);
	}

	/// <summary>
	/// Creates an object creation expression: new Type(args)
	/// </summary>
	public static ObjectCreationExpressionSyntax ObjectCreation(TypeSyntax type, params ExpressionSyntax[] arguments)
	{
		return SyntaxFactory.ObjectCreationExpression(type)
			.WithArgumentList(
				SyntaxFactory.ArgumentList(
					SyntaxFactory.SeparatedList(
						arguments.Select(SyntaxFactory.Argument))));
	}

	/// <summary>
	/// Creates an element access expression: target[index]
	/// </summary>
	public static ElementAccessExpressionSyntax ElementAccess(ExpressionSyntax target, ExpressionSyntax index)
	{
		return SyntaxFactory.ElementAccessExpression(target)
			.WithArgumentList(
				SyntaxFactory.BracketedArgumentList(
					SyntaxFactory.SingletonSeparatedList(
						SyntaxFactory.Argument(index))));
	}
}
