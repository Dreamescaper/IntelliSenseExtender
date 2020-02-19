using System.Collections.Generic;
using System.Linq;
using IntelliSenseExtender.Context;
using IntelliSenseExtender.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public class ExtensionMethodsCompletionProvider : ITypeCompletionProvider
    {
        public IEnumerable<CompletionItem>? GetCompletionItemsForType(INamedTypeSymbol typeSymbol, SyntaxContext syntaxContext, Options.Options options)
        {
            if (!options.SuggestUnimportedExtensionMethods)
                return null;

            if (syntaxContext.IsNamespaceImported(typeSymbol.ContainingNamespace)
                || !typeSymbol.MightContainExtensionMethods)
            {
                return null;
            }

            var extMethodSymbols = typeSymbol
                .GetMembers()
                .OfType<IMethodSymbol>()
                .Where(methodSymbol => syntaxContext.IsAccessible(methodSymbol)
                    && !(options.FilterOutObsoleteSymbols && methodSymbol.IsObsolete()))
                .Select(m => m.ReduceExtensionMethod(syntaxContext.AccessedSymbolType!))
                .Where(m => m != null);

            return extMethodSymbols.Select(s => CreateCompletionItemForSymbol(s!, syntaxContext));
        }

        public bool IsApplicable(SyntaxContext syntaxContext, Options.Options options)
        {
            return options.SuggestUnimportedExtensionMethods
                && syntaxContext.IsMemberAccessContext
                && syntaxContext.AccessedSymbolType != null
                && syntaxContext.AccessedSymbol?.Kind != SymbolKind.NamedType;
        }

        private CompletionItem CreateCompletionItemForSymbol(ISymbol typeSymbol, SyntaxContext context)
        {
            return CompletionItemHelper.CreateCompletionItem(typeSymbol, context, Sorting.Default);
        }
    }
}
