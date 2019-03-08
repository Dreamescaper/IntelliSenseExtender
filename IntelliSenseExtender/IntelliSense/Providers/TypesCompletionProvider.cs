using System.Collections.Generic;
using System.Diagnostics;
using IntelliSenseExtender.Context;
using IntelliSenseExtender.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public class TypesCompletionProvider : ITypeCompletionProvider
    {
        public IEnumerable<CompletionItem> GetCompletionItemsForType(INamedTypeSymbol typeSymbol, SyntaxContext syntaxContext, Options.Options options)
        {
            var typesSw = Stopwatch.StartNew();

            // Only attributes are permitted in attribute context
            if (syntaxContext.IsAttributeContext && (typeSymbol.IsAbstract || !typeSymbol.IsAttribute()))
                return null;

            // Skip nested type if there is static using for parent type 
            if (typeSymbol.ContainingType != null && syntaxContext.StaticImports.Contains(typeSymbol.ContainingType))
                return null;

            if (!syntaxContext.IsNamespaceImported(typeSymbol.ContainingNamespace)
                && !syntaxContext.Aliases.ContainsKey(typeSymbol))
            {
                PerfMetric.Types += typesSw.Elapsed;
                return new[] { CreateCompletionItemForSymbol(typeSymbol, syntaxContext, options) };
            }
            // If nested types suggestions are enabled, we should return imported suggestions as well
            else if (options.SuggestNestedTypes && typeSymbol.ContainingType != null)
            {
                PerfMetric.Types += typesSw.Elapsed;
                return new[] { CompletionItemHelper.CreateCompletionItem(typeSymbol, syntaxContext, unimported: false) };
            }

            PerfMetric.Types += typesSw.Elapsed;
            return null;
        }

        public bool IsApplicable(SyntaxContext syntaxContext, Options.Options options)
        {
            return syntaxContext.IsTypeContext;
        }

        private CompletionItem CreateCompletionItemForSymbol(ISymbol typeSymbol, SyntaxContext context, Options.Options options)
        {
            int sorting = options.SortCompletionsAfterImported ? Sorting.Last : Sorting.Default;
            return CompletionItemHelper.CreateCompletionItem(typeSymbol, context, sorting);
        }
    }
}
