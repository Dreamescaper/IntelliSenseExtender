using System.Collections.Generic;
using System.Threading.Tasks;
using IntelliSenseExtender.IntelliSense.Context;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.IntelliSense.Providers.Interfaces
{
    public interface ICompletionProvider
    {
        Task<IEnumerable<CompletionItem>> GetCompletionItemsAsync(SyntaxContext syntaxContext, Options.Options options);
        bool IsApplicable(SyntaxContext syntaxContext, Options.Options options);
    }
}
