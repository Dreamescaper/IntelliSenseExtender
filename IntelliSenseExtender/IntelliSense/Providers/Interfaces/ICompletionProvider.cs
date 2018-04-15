using IntelliSenseExtender.Context;

namespace IntelliSenseExtender.IntelliSense.Providers.Interfaces
{
    public interface ICompletionProvider
    {
        bool IsApplicable(SyntaxContext syntaxContext, Options.Options options);
    }
}
