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

        public static bool IsAttribute(this INamedTypeSymbol typeSymbol)
        {
            var currentSymbol = typeSymbol.BaseType;
            while (currentSymbol != null)
            {
                if (currentSymbol.Name == "Attribute"
                    && currentSymbol.ContainingNamespace?.Name == "System")
                {
                    return true;
                }

                currentSymbol = currentSymbol.BaseType;
            }
            return false;
        }
    }
}
