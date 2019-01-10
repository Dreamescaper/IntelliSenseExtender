using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace IntelliSenseExtender.Extensions
{
    public static class SymbolExtensions
    {
        private const int DefaultSize = 8;

        public static string GetNamespace(this ISymbol symbol)
        {
            // ToDisplayString would work here as well, but it is slower
            var nsNames = new Stack<string>(DefaultSize);

            while (symbol != null)
            {
                if (symbol is INamespaceSymbol nsSymbol)
                {
                    if (nsSymbol.IsGlobalNamespace)
                    {
                        break;
                    }

                    nsNames.Push(symbol.Name);
                }
                symbol = symbol.ContainingSymbol;
            }

            return string.Join(".", nsNames.ToArray());
        }

        public static string GetFullyQualifiedName(this ISymbol symbol, string @namespace = null)
        {
            if (symbol is ITypeSymbol)
            {
                // ToDisplayString would work in this case as well, but it is slower
                @namespace = @namespace ?? symbol.GetNamespace();
                return string.IsNullOrEmpty(@namespace)
                    ? symbol.Name
                    : $"{@namespace}.{symbol.Name}";
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
            return typeSymbol.SpecialType >= SpecialType.System_Object
                && typeSymbol.SpecialType <= SpecialType.System_Array;
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