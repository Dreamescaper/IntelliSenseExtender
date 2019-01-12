using System.Collections.Generic;
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
            if (syntaxContext.InferredType is INamedTypeSymbol namedType
                && namedType.EnumUnderlyingType != null)
            {
                return new[]
                {
                    CompletionItemHelper.CreateCompletionItem(namedType, syntaxContext,
                        unimported: !syntaxContext.IsNamespaceImported(namedType.ContainingNamespace),
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
