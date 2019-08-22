using System.Collections.Generic;
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
            // Only attributes are permitted in attribute context
            if (syntaxContext.IsAttributeContext && (typeSymbol.IsAbstract || !typeSymbol.IsAttribute()))
                return null;

            // Skip nested type if there is static using for parent type 
            if (typeSymbol.ContainingType != null && syntaxContext.StaticImports.Contains(typeSymbol.ContainingType))
                return null;

            // Skip if alias present from current type
            if (syntaxContext.Aliases.ContainsKey(typeSymbol))
                return null;

            var isImported = syntaxContext.IsNamespaceImported(typeSymbol.ContainingNamespace);

            // Add nested type independently whether imported or not
            if (options.SuggestNestedTypes && typeSymbol.ContainingType != null)
            {
                int sorting = (!isImported && options.SortCompletionsAfterImported)
                    ? Sorting.Last
                    : Sorting.Default;

                return new[] { CompletionItemHelper.CreateCompletionItem(typeSymbol, syntaxContext, sorting, unimported: !isImported) };
            }
            else if (options.SuggestUnimportedTypes && !isImported)
            {
                int sorting = options.SortCompletionsAfterImported ? Sorting.Last : Sorting.Default;
                return new[] { CompletionItemHelper.CreateCompletionItem(typeSymbol, syntaxContext, sorting) };
            }

            return null;
        }

        public bool IsApplicable(SyntaxContext syntaxContext, Options.Options options)
        {
            return (options.SuggestUnimportedTypes || options.SuggestNestedTypes)
                && syntaxContext.IsTypeContext;
        }
    }
}
