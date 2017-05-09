using System;
using System.Linq;
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

        public static bool IsObsolete(this ISymbol symbol)
        {
            return symbol
                .GetAttributes()
                .Any(attribute => attribute.AttributeClass.Name == nameof(ObsoleteAttribute));
        }

        public static bool IsAttribute(this INamedTypeSymbol typeSymbol)
        {
            var currentSymbol = typeSymbol.BaseType;
            while (currentSymbol != null)
            {
                if (currentSymbol.Name == nameof(Attribute)
                    && currentSymbol.ContainingNamespace?.Name == nameof(System))
                {
                    return true;
                }

                currentSymbol = currentSymbol.BaseType;
            }
            return false;
        }
    }
}
