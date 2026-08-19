using Microsoft.CodeAnalysis.CSharp.Syntax;
using NodeDev.Core.Types;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace NodeDev.Core.CodeGeneration;

/// <summary>
/// Minimal helper for truly shared Roslyn syntax generation across multiple nodes.
/// Node-specific syntax generation should be done directly in the node classes.
/// </summary>
internal static class RoslynHelpers
{
	/// <summary>
	/// Creates a TypeSyntax from a TypeBase. Used across multiple nodes for type resolution.
	/// </summary>
	internal static TypeSyntax GetTypeSyntax(TypeBase type)
	{
		var typeName = type.FriendlyName;

		// Handle array types
		if (type is NodeClassArrayType arrayType)
		{
			var elementType = GetTypeSyntax(arrayType.ArrayInnerType);
			return SF.ArrayType(elementType)
				.WithRankSpecifiers(
					SF.SingletonList(
						SF.ArrayRankSpecifier(
							SF.SingletonSeparatedList<ExpressionSyntax>(
								SF.OmittedArraySizeExpression()))));
		}

		// Parse the type name - handles generics like "List<int>"
		return SF.ParseTypeName(typeName);
	}

	/// <summary>
	/// Creates a globally-qualified syntax node for a supported BCL Action or Func
	/// type. Lambda casts use this form so a project type named Action or Func cannot
	/// shadow the intended delegate family.
	/// </summary>
	internal static TypeSyntax GetExactDelegateTypeSyntax(TypeBase delegateType)
	{
		if (delegateType is not RealType realType)
			throw new ArgumentException($"Delegate type must be a real BCL type, but was {delegateType.FriendlyName}.", nameof(delegateType));

		var backendType = realType.BackendType;
		var typeName = backendType.Name.Split('`')[0];
		if (backendType.Namespace != "System" || (typeName != nameof(Action) && typeName != "Func"))
			throw new ArgumentException($"Unsupported delegate type {delegateType.FriendlyName}.", nameof(delegateType));

		SimpleNameSyntax simpleName = realType.Generics.Length == 0
			? SF.IdentifierName(typeName)
			: SF.GenericName(SF.Identifier(typeName))
				.WithTypeArgumentList(
					SF.TypeArgumentList(
						SF.SeparatedList(realType.Generics.Select(GetTypeSyntax))));

		return SF.QualifiedName(SF.ParseName("global::System"), simpleName);
	}
}
