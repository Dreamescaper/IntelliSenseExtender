using System.Collections.Generic;
using IntelliSenseExtender.Context;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.IntelliSense.Providers.Interfaces
{
    public interface ICompletionProvider
    {
        IEnumerable<CompletionItem>? GetCompletionItems(SyntaxContext syntaxContext, Options.Options options);
        bool IsApplicable(SyntaxContext syntaxContext, Options.Options options);
    }
}
