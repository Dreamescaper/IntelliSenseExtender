using System.Threading.Tasks;
using IntelliSenseExtender.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    [ExportCompletionProvider(nameof(LocalsCompletionProvider), LanguageNames.CSharp)]
    public class LocalsCompletionProvider : AbstractCompletionProvider
    {
        public LocalsCompletionProvider() : base()
        {
        }

        public LocalsCompletionProvider(IOptionsProvider optionsProvider) : base(optionsProvider)
        {
        }

        public override Task ProvideCompletionsAsync(CompletionContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
