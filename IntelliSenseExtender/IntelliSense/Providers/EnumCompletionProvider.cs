using System;
using System.Collections.Generic;
using System.Linq;
using IntelliSenseExtender.Context;
using IntelliSenseExtender.IntelliSense.Providers.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public class EnumCompletionProvider : ISimpleCompletionProvider
    {
        public IEnumerable<CompletionItem> GetCompletionItems(SyntaxContext syntaxContext, Options.Options options)
        {
            var typeSymbol = syntaxContext.InferredType;

            // Unwrap nullable enum
            if (typeSymbol is INamedTypeSymbol namedType
                && namedType.Name == nameof(Nullable)
                && namedType.TypeArguments.FirstOrDefault()?.TypeKind == TypeKind.Enum)
            {
                typeSymbol = namedType.TypeArguments[0];
            }

            if (typeSymbol?.TypeKind == TypeKind.Enum)
            {
                return new[]
                {
                    CompletionItemHelper.CreateCompletionItem(typeSymbol, syntaxContext,
                        unimported: !syntaxContext.IsNamespaceImported(typeSymbol.ContainingNamespace),
                        matchPriority: MatchPriority.Preselect,
                        sortingPriority: Sorting.Suitable_Enum)
                };
            }

            return null;
        }

        public bool IsApplicable(SyntaxContext syntaxContext, Options.Options options)
        {
            return true;
        }
    }
}
