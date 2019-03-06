using System.Collections.Generic;
using System.Linq;
using IntelliSenseExtender.Context;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.IntelliSense.Providers.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public class TypesCompletionProvider : ISimpleCompletionProvider
    {
        private static List<(INamedTypeSymbol symbol, CompletionItem item)> typeCompletionItems;

        public IEnumerable<CompletionItem> GetCompletionItems(SyntaxContext syntaxContext, Options.Options options)
        {
            foreach (var (typeSymbol, completionItem) in typeCompletionItems)
            {
                // Only attributes are permitted in attribute context
                if (syntaxContext.IsAttributeContext && (typeSymbol.IsAbstract || !typeSymbol.IsAttribute()))
                    continue;

                // Skip nested type if there is static using for parent type 
                if (typeSymbol.ContainingType != null && syntaxContext.StaticImports.Contains(typeSymbol.ContainingType))
                    continue;

                if (!syntaxContext.IsNamespaceImported(typeSymbol.ContainingNamespace)
                    && !syntaxContext.Aliases.ContainsKey(typeSymbol))
                {
                    yield return completionItem;
                }
                // If nested types suggestions are enabled, we should return imported suggestions as well
                else if (options.SuggestNestedTypes && typeSymbol.ContainingType != null)
                {
                    // need to re-create item, as it should not add namespace
                    yield return CompletionItemHelper.CreateCompletionItem(typeSymbol, syntaxContext, unimported: false);
                }
            }
        }

        internal static void CreateTypeCompletions(SyntaxContext syntaxContext, Options.Options options)
        {
            typeCompletionItems = SymbolNavigator.GetAllTypes(syntaxContext, options)
                .Select(typeSymbol =>
                (
                    typeSymbol,
                    CreateCompletionItemForSymbol(typeSymbol, syntaxContext, options))
                )
                .ToList();
        }

        public bool IsApplicable(SyntaxContext syntaxContext, Options.Options options)
        {
            return syntaxContext.IsTypeContext;
        }

        private static CompletionItem CreateCompletionItemForSymbol(ISymbol typeSymbol, SyntaxContext context, Options.Options options)
        {
            int sorting = options.SortCompletionsAfterImported ? Sorting.Last : Sorting.Default;
            return CompletionItemHelper.CreateCompletionItem(typeSymbol, context, sorting);
        }
    }
}
