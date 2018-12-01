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
            // ToDisplayString would work here as well, but it is slower
            var nsNames = new LinkedList<string>();

            while (symbol != null)
            {
                if (symbol is INamespaceSymbol nsSymbol)
                {
                    if (nsSymbol.IsGlobalNamespace)
                    {
                        break;
                    }

                    nsNames.AddFirst(symbol.Name);
                }
                symbol = symbol.ContainingSymbol;
            }

            return string.Join(".", nsNames);
        }

        public static string GetFullyQualifiedName(this ISymbol symbol)
        {
            if (symbol is ITypeSymbol)
            {
                // ToDisplayString would work in this case as well, but it is slower
                var nsName = symbol.GetNamespace();
                return string.IsNullOrEmpty(nsName)
                    ? symbol.Name
                    : $"{nsName}.{symbol.Name}";
            }
            else
            {
                return symbol.ToDisplayString();
            }
        }

        /// <summary>
        /// Get name including containing type name, if present
        /// </summary>
        public static string GetAccessibleName(this ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol typeSymbol)
            {
                var containingType = typeSymbol.ContainingType;
                if (containingType != null)
                {
                    return $"{containingType.GetAccessibleName()}.{symbol.Name}";
                }
            }
            return symbol.Name;
        }

        public static bool IsObsolete(this ISymbol symbol)
        {
            return symbol
                .GetAttributes()
                .Any(attribute => attribute.AttributeClass.Name == nameof(ObsoleteAttribute));
        }

        public static IEnumerable<ISymbol> AncestorsAndSelf(this ISymbol symbol)
        {
            while (symbol != null)
            {
                yield return symbol;
                symbol = symbol.ContainingSymbol;
            }
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