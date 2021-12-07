using System.Collections.Generic;
using System.Threading.Tasks;
using IntelliSenseExtender.IntelliSense.Context;
using IntelliSenseExtender.IntelliSense.Providers.Interfaces;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public class NestedTypesCompletionProvider : ICompletionProvider
    {
        public Task<IEnumerable<CompletionItem>> GetCompletionItemsAsync(SyntaxContext syntaxContext, Options.Options options)
        {
            return Task.FromResult(GetCompletionItems(syntaxContext));
        }

        private IEnumerable<CompletionItem> GetCompletionItems(SyntaxContext syntaxContext)
        {
            var hasStaticImports = syntaxContext.StaticImports.Count > 0;
            var hasAliases = syntaxContext.Aliases.Count > 0;

            foreach (var typeSymbol in SymbolNavigator.GetAllTypes(syntaxContext))
            {
                if (typeSymbol.ContainingType == null)
                    continue;

                // Skip nested type if there is static using for parent type 
                if (hasStaticImports && syntaxContext.StaticImports.Contains(typeSymbol.ContainingType))
                    continue;

                // Skip if alias present from current type
                if (hasAliases && syntaxContext.Aliases.ContainsKey(typeSymbol))
                    continue;

                var isImported = syntaxContext.IsNamespaceImported(typeSymbol.ContainingNamespace);

                var sorting = isImported ? Sorting.Default : Sorting.Last;

                yield return CompletionItemHelper.CreateCompletionItem(typeSymbol, syntaxContext, sorting,
                    unimported: !isImported);
            }
        }

        public bool IsApplicable(SyntaxContext syntaxContext, Options.Options options)
        {
            return options.SuggestNestedTypes
                && syntaxContext.IsTypeContext
                && !syntaxContext.IsAttributeContext;
        }
    }
}
