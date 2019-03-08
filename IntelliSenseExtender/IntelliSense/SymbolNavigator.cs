using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IntelliSenseExtender.Context;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.IntelliSense.Providers;
using Microsoft.CodeAnalysis;

namespace IntelliSenseExtender.IntelliSense
{
    public class SymbolNavigator
    {
        public static IEnumerable<INamedTypeSymbol> GetAllTypes(SyntaxContext syntaxContext, Options.Options options)
        {
            var sw = Stopwatch.StartNew();

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
                        if (syntaxContext.IsAccessible(namedTypeSymbol)
                            && !(options.FilterOutObsoleteSymbols && namedTypeSymbol.IsObsolete()))
                        {
                            PerfMetric.TotalTypesCount++;
                            PerfMetric.TraverseTypes += sw.Elapsed;
                            yield return namedTypeSymbol;
                            sw = Stopwatch.StartNew();

                            if (options.SuggestNestedTypes)
                            {
                                symbolsToTraverse.Enqueue(namedTypeSymbol);
                            }
                        }
                    }
                    else if (member is INamespaceSymbol ns)
                    {
                        symbolsToTraverse.Enqueue(ns);
                    }
                }
            }

            PerfMetric.TraverseTypes += sw.Elapsed;
        }

        public static ISymbol FindSymbolByFullName(ISymbol parent, string fullName)
        {
            var pathNames = fullName.Split('.');
            ISymbol currentSymbol = parent;

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
