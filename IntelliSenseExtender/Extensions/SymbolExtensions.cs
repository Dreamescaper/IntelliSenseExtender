using System.Collections.Concurrent;
using System.Collections.Generic;
using IntelliSenseExtender.IntelliSense.Context;
using Microsoft.CodeAnalysis;

namespace IntelliSenseExtender.Extensions
{
    public static class SymbolExtensions
    {
        private static readonly ConcurrentBag<Stack<string>> stackPool = new ConcurrentBag<Stack<string>>();
        private const int DefaultSize = 8;

        public static string GetNamespace(this ISymbol symbol)
        {
            // ToDisplayString would work here as well, but it is slower
            if (!stackPool.TryTake(out var nsNames))
                nsNames = new Stack<string>(DefaultSize);

            try
            {
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
            finally
            {
                nsNames.Clear();
                stackPool.Add(nsNames);
            }
        }

        public static string GetFullyQualifiedName(this ISymbol symbol, string? @namespace = null)
        {
            if (symbol is ITypeSymbol)
            {
                // ToDisplayString would work in this case as well, but it is slower
                @namespace ??= symbol.GetNamespace();
                return string.IsNullOrEmpty(@namespace)
                    ? symbol.Name
                    : $"{@namespace}.{symbol.Name}";
            }
            if (symbol is INamespaceSymbol)
            {
                return symbol.GetNamespace();
            }
            if (symbol.ContainingType != null)
            {
                var typeFullName = symbol.ContainingType.GetFullyQualifiedName(@namespace);
                return $"{typeFullName}.{symbol.Name}";
            }
            else
            {
                return symbol.ToDisplayString();
            }
        }

        /// <summary>
        /// Get name including containing type name, if present
        /// </summary>
        public static string GetAccessibleName(this ISymbol symbol, SyntaxContext syntaxContext)
        {
            if (symbol is INamedTypeSymbol typeSymbol)
            {
                var containingType = typeSymbol.ContainingType;
                if (containingType != null && !syntaxContext.StaticImports.Contains(containingType))
                {
                    return $"{containingType.GetAccessibleName(syntaxContext)}.{symbol.Name}";
                }
            }
            return symbol.Name;
        }

        public static IEnumerable<ISymbol> AncestorsAndSelf(this ISymbol symbol)
        {
            while (symbol != null)
            {
                yield return symbol;
                symbol = symbol.ContainingSymbol;
            }
        }

        public static bool IsBuiltInType(this ITypeSymbol typeSymbol)
        {
            return typeSymbol.SpecialType >= SpecialType.System_Object
                && typeSymbol.SpecialType <= SpecialType.System_Array;
        }
    }
}