using System;
using System.Collections.Generic;
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
            var baseTypes = typeSymbol.GetBaseTypes();
            return baseTypes.Any(baseTypeSymbol => baseTypeSymbol.Name == nameof(Attribute)
                    && baseTypeSymbol.ContainingNamespace?.Name == nameof(System));
        }

        public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol typeSymbol)
        {
            var currentSymbol = typeSymbol.BaseType;
            while (currentSymbol != null)
            {
                yield return currentSymbol;
                currentSymbol = currentSymbol.BaseType;
            }
        }

        public static bool IsAssignableFrom(this ITypeSymbol toSymbol, ITypeSymbol fromSymbol)
        {
            var assignableTypes = new[] { fromSymbol }.Concat(fromSymbol.AllInterfaces).Concat(fromSymbol.GetBaseTypes());
            return assignableTypes.Contains(toSymbol);
        }
    }
}
