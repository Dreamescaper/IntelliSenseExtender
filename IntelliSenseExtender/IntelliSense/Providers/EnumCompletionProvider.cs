using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IntelliSenseExtender.IntelliSense.Context;
using IntelliSenseExtender.IntelliSense.Providers.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public class EnumCompletionProvider : ICompletionProvider
    {
        public Task<IEnumerable<CompletionItem>> GetCompletionItemsAsync(SyntaxContext syntaxContext, Options.Options options)
        {
            var typeSymbol = syntaxContext.InferredInfo.Type;

            // Unwrap nullable enum
            if (typeSymbol is INamedTypeSymbol namedType
                && namedType.Name == nameof(Nullable)
                && namedType.TypeArguments.FirstOrDefault()?.TypeKind == TypeKind.Enum)
            {
                typeSymbol = namedType.TypeArguments[0];
            }

            if (typeSymbol?.TypeKind == TypeKind.Enum)
            {
                return Task.FromResult(new[]
                {
                    CompletionItemHelper.CreateCompletionItem(typeSymbol, syntaxContext,
                        unimported: !syntaxContext.IsNamespaceImported(typeSymbol.ContainingNamespace),
                        matchPriority: MatchPriority.Preselect,
                        sortingPriority: Sorting.Suitable_Enum)
                }.AsEnumerable());
            }

            return Task.FromResult(Enumerable.Empty<CompletionItem>());
        }

        public bool IsApplicable(SyntaxContext syntaxContext, Options.Options options)
        {
            return true;
        }
    }
}
