using System.Collections.Generic;
using IntelliSenseExtender.Context;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public class NestedTypesCompletionProvider : ITypeCompletionProvider
    {
        public IEnumerable<CompletionItem>? GetCompletionItemsForType(INamedTypeSymbol typeSymbol, SyntaxContext syntaxContext, Options.Options options)
        {
            // Only attributes are permitted in attribute context
            if (syntaxContext.IsAttributeContext)
                return null;

            if (typeSymbol.ContainingType == null)
                return null;

            // Skip nested type if there is static using for parent type 
            if (syntaxContext.StaticImports.Contains(typeSymbol.ContainingType))
                return null;

            // Skip if alias present from current type
            if (syntaxContext.Aliases.ContainsKey(typeSymbol))
                return null;

            var isImported = syntaxContext.IsNamespaceImported(typeSymbol.ContainingNamespace);

            var sorting = isImported ? Sorting.Default : Sorting.Last;
            return new[] { CompletionItemHelper.CreateCompletionItem(typeSymbol, syntaxContext, sorting,
                unimported: !isImported) };
        }

        public bool IsApplicable(SyntaxContext syntaxContext, Options.Options options)
        {
            return options.SuggestNestedTypes && syntaxContext.IsTypeContext;
        }
    }
}
