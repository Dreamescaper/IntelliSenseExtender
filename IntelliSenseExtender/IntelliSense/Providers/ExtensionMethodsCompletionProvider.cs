using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using IntelliSenseExtender.Context;
using IntelliSenseExtender.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public class ExtensionMethodsCompletionProvider : ITypeCompletionProvider
    {
        public IEnumerable<CompletionItem> GetCompletionItemsForType(INamedTypeSymbol typeSymbol, SyntaxContext syntaxContext, Options.Options options)
        {
            var extMethodsSw = Stopwatch.StartNew();

            if (syntaxContext.IsNamespaceImported(typeSymbol.ContainingNamespace)
                || !typeSymbol.MightContainExtensionMethods)
            {
                return null;
            }

            var extMethodSymbols = typeSymbol
                .GetMembers()
                .OfType<IMethodSymbol>()
                .Where(symbol => !(options.FilterOutObsoleteSymbols && symbol.IsObsolete()))
                .Select(m => m.ReduceExtensionMethod(syntaxContext.AccessedSymbolType))
                .Where(m => m != null)
                .ToList();

            PerfMetric.ExtMethods += extMethodsSw.Elapsed;

            return extMethodSymbols.Select(s => CreateCompletionItemForSymbol(s, syntaxContext, options));
        }

        public bool IsApplicable(SyntaxContext syntaxContext, Options.Options options)
        {
            return syntaxContext.IsMemberAccessContext
                && syntaxContext.AccessedSymbolType != null
                && syntaxContext.AccessedSymbol?.Kind != SymbolKind.NamedType;
        }

        private CompletionItem CreateCompletionItemForSymbol(ISymbol typeSymbol, SyntaxContext context, Options.Options options)
        {
            int sorting = options.SortCompletionsAfterImported ? Sorting.Last : Sorting.Default;
            return CompletionItemHelper.CreateCompletionItem(typeSymbol, context, sorting);
        }
    }
}
