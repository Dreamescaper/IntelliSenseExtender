using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace IntelliSenseExtender.Extensions
{
    public static class SymbolExtensions
    {
        private static readonly HashSet<string> BuiltInTypes = new HashSet<string>(new[] { "Byte", "SByte", "Int32",
            "UInt32", "Int16", "UInt16", "Int64", "UInt64", "Single", "Double", "Char",
            "Boolean", "Object", "String", "Decimal" });

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

        public static bool IsBuiltInType(this ITypeSymbol typeSymbol)
        {
            return BuiltInTypes.Contains(typeSymbol.Name);
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
    }
}