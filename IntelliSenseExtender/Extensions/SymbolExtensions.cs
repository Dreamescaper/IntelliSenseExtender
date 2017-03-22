using Microsoft.CodeAnalysis;

namespace IntelliSenseExtender.Extensions
{
    public static class SymbolExtensions
    {
        public static string GetNamespace(this ISymbol symbol)
        {
            return symbol.ContainingNamespace.ToDisplayString();
        }

        public static string GetFullyQualifiedName(this ISymbol symbol)
        {
            return symbol.ToDisplayString();
        }
    }
}
