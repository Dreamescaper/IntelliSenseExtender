using System.Collections.Generic;
using System.Linq;
using IntelliSenseExtender.IntelliSense.Context;
using Microsoft.CodeAnalysis;

namespace IntelliSenseExtender.IntelliSense
{
    public class SymbolNavigator
    {
        public static IEnumerable<INamedTypeSymbol> GetAllTypes(SyntaxContext syntaxContext)
        {
            var symbolsToTraverse = new Queue<INamespaceOrTypeSymbol>();

            var globalNamespace = syntaxContext.SemanticModel.Compilation.GlobalNamespace;
            symbolsToTraverse.Enqueue(globalNamespace);

            while (symbolsToTraverse.Count > 0)
            {
                var current = symbolsToTraverse.Dequeue();

                foreach (var member in current.GetMembers())
                {
                    if (member is INamedTypeSymbol namedTypeSymbol)
                    {
                        if (syntaxContext.IsAccessible(namedTypeSymbol))
                        {
                            yield return namedTypeSymbol;
                            symbolsToTraverse.Enqueue(namedTypeSymbol);
                        }
                    }
                    else if (member is INamespaceSymbol ns)
                    {
                        symbolsToTraverse.Enqueue(ns);
                    }
                }
            }
        }

        public static ISymbol? FindSymbolByFullName(ISymbol parent, string fullName)
        {
            var pathNames = fullName.Split('.');
            ISymbol? currentSymbol = parent;

            foreach (var name in pathNames)
            {
                currentSymbol = (currentSymbol as INamespaceOrTypeSymbol)?.GetMembers(name).FirstOrDefault();

                if (currentSymbol == null)
                    return null;
            }

            return currentSymbol;
        }
    }
}
