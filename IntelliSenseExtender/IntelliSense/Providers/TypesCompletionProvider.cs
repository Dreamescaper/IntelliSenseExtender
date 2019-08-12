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
                var sorting = isImported ? Sorting.Default : Sorting.Last;
                return new[] { CompletionItemHelper.CreateCompletionItem(typeSymbol, syntaxContext, sorting,
                    unimported: !isImported) };
            }
            else if (options.SuggestUnimportedTypes && !isImported)
            {
                return new[] { CreateCompletionItemForSymbol(typeSymbol, syntaxContext) };
            }

            return null;
        }

        public bool IsApplicable(SyntaxContext syntaxContext, Options.Options options)
        {
            return (options.SuggestUnimportedTypes || options.SuggestNestedTypes)
                && syntaxContext.IsTypeContext;
        }

        private CompletionItem CreateCompletionItemForSymbol(ISymbol typeSymbol, SyntaxContext context)
        {
            return CompletionItemHelper.CreateCompletionItem(typeSymbol, context, Sorting.Last);
        }
    }
}
