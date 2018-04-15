using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

namespace IntelliSenseExtender.IntelliSense.Providers.Interfaces
{
    public interface ITriggerCompletions
    {
        bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, Options.Options options);
    }
}
