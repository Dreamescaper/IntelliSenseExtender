using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntelliSenseExtender.Extensions
{
    public static class SyntaxTokenExtensions
    {
        public static bool IsMemberAccessContext(this SyntaxToken currentToken,
            [NotNullWhen(true)] out ExpressionSyntax? accessedExpressionSyntax)
        {
            accessedExpressionSyntax = null;

            var parentNode = currentToken.Parent;
            if (parentNode is IdentifierNameSyntax)
            {
                parentNode = parentNode.Parent;
            }

            if (parentNode is MemberAccessExpressionSyntax memberAccessNode)
            {
                accessedExpressionSyntax = memberAccessNode.Expression;
            }
            else if (parentNode?.Parent is ConditionalAccessExpressionSyntax conditionalAccessNode)
            {
                accessedExpressionSyntax = conditionalAccessNode.Expression;
            }

            return accessedExpressionSyntax != null;
        }

        public static bool IsObjectCreationContext(this SyntaxToken currentToken,
            [NotNullWhen(true)] out ObjectCreationExpressionSyntax? creationExpressionSyntax)
        {
            creationExpressionSyntax = null;

            if (currentToken.Kind() == SyntaxKind.NewKeyword)
            {
                creationExpressionSyntax = currentToken.Parent as ObjectCreationExpressionSyntax;
            }
            return creationExpressionSyntax != null;
        }
    }
}
