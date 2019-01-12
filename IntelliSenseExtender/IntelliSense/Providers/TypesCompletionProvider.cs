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
            if ((!syntaxContext.IsAttributeContext || (typeSymbol.IsAttribute() && !typeSymbol.IsAbstract))
                && (typeSymbol.ContainingType == null || !syntaxContext.StaticImports.Contains(typeSymbol.ContainingType)))
            {
                if (!syntaxContext.IsNamespaceImported(typeSymbol.ContainingNamespace)
                    && !syntaxContext.Aliases.ContainsKey(typeSymbol))
                {
                    return new[] { CreateCompletionItemForSymbol(typeSymbol, syntaxContext, options) };
                }
                // If nested types suggestions are enabled, we should return imported suggestions as well
                else if (options.SuggestNestedTypes && typeSymbol.ContainingType != null)
                {
                    return new[] { CompletionItemHelper.CreateCompletionItem(typeSymbol, syntaxContext, unimported: false) };
                }
            }

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
