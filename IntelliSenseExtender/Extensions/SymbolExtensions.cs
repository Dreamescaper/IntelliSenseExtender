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

        public static bool IsAssignableFrom(this ITypeSymbol baseSymbol, ITypeSymbol derivedSymbol)
        {
            if (!(baseSymbol is INamedTypeSymbol baseTypeSymbol
                && derivedSymbol is INamedTypeSymbol derivedTypeSymbol))
            {
                return false;
            }

            return GetAssignableTypeSymbols(derivedTypeSymbol)
                .Any(ts => ts.MetadataName == baseTypeSymbol.MetadataName
                    && (!ts.IsGenericType || IsGenericTypeParametersCompatible(baseTypeSymbol, ts)));
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

        private static IEnumerable<INamedTypeSymbol> GetAssignableTypeSymbols(INamedTypeSymbol currentTypeSymbol)
        {
            while (currentTypeSymbol != null)
            {
                yield return currentTypeSymbol;
                foreach (var i in currentTypeSymbol.Interfaces)
                {
                    yield return i;
                }
                currentTypeSymbol = currentTypeSymbol.BaseType;
            }
        }

        /// <summary>
        /// Returns true if we can provide derivedTypeSymbol as parameter for baseTypeSymbol parameter type.
        /// </summary>
        private static bool IsGenericTypeParametersCompatible(INamedTypeSymbol baseTypeSymbol, INamedTypeSymbol derivedTypeSymbol)
        {
            if (baseTypeSymbol.Arity != derivedTypeSymbol.Arity)
            {
                return false;
            }

            for (int i = 0; i < baseTypeSymbol.Arity; i++)
            {
                var baseTypeArgument = baseTypeSymbol.TypeArguments[i];
                var derivedTypeArgument = derivedTypeSymbol.TypeArguments[i];

                // generic 'type' has NotApplicable accessability
                // TODO: it seems to be working, but looks strange. Find better approach
                // TODO: add support for 'where' conditions
                bool compatible = baseTypeArgument.DeclaredAccessibility == Accessibility.NotApplicable
                    || baseTypeArgument == derivedTypeArgument;
                if (!compatible)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
