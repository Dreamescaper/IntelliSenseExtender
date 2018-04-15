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
            if (!syntaxContext.IsTypeContext
               || (syntaxContext.IsAttributeContext && (!typeSymbol.IsAttribute() || typeSymbol.IsAbstract))
               || syntaxContext.IsNamespaceImported(typeSymbol))
            {
                return null;
            }

            return new[] { CreateCompletionItemForSymbol(typeSymbol, syntaxContext, options) };
        }

        private CompletionItem CreateCompletionItemForSymbol(ISymbol typeSymbol, SyntaxContext context, Options.Options options)
        {
            int sorting = options.SortCompletionsAfterImported ? Sorting.Last : Sorting.Default;
            return CompletionItemHelper.CreateCompletionItem(typeSymbol, context, sorting);
        }
    }
}
