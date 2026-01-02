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
}
