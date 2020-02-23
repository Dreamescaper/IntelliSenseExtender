using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.Context;
using IntelliSenseExtender.IntelliSense.Providers.Interfaces;
using IntelliSenseExtender.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    [ExportCompletionProvider(nameof(AggregateTypeCompletionProvider), LanguageNames.CSharp)]
    public class AggregateTypeCompletionProvider : CompletionProvider
    {
        private readonly IOptionsProvider _optionsProvider;

        private readonly ICompletionProvider[] completionProviders;
        private readonly ITriggerCompletions[] triggerCompletions;

        private bool _triggeredByUs = false;

        public AggregateTypeCompletionProvider()
            : this(VsSettingsOptionsProvider.Current,
                  new NestedTypesCompletionProvider(),
                  new LocalsCompletionProvider(),
                  new NewObjectCompletionProvider(),
                  new EnumCompletionProvider())
        {
        }

        public AggregateTypeCompletionProvider(IOptionsProvider optionsProvider, params ICompletionProvider[] completionProviders)
        {
            this.completionProviders = completionProviders;
            triggerCompletions = completionProviders.OfType<ITriggerCompletions>().ToArray();

            _optionsProvider = optionsProvider;
        }

        public Options.Options? Options => _optionsProvider.GetOptions();

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
            {
                if (Options == null)
                {
                    // Package not loaded yet (e.g. no solution opened)
                    return;
                }
                if (await IsWatchWindowAsync(context).ConfigureAwait(false))
                {
                    // Completions are not usable in watch window
                    return;
                }

                if (IsRazorView(context))
                {
                    // Completions are not usable in cshtml-Razor Views. Insertion of Namespaces doesn't work there.
                    return;
                }

                var syntaxContext = await SyntaxContext.CreateAsync(context.Document, context.Position, context.CancellationToken)
                    .ConfigureAwait(false);

                if (syntaxContext == null)
                    return;

                var completionsTasks = completionProviders
                    .Where(p => p.IsApplicable(syntaxContext, Options))
                    .Select(provider => provider.GetCompletionItemsAsync(syntaxContext, Options));

                var completions = (await Task.WhenAll(completionsTasks).ConfigureAwait(false)).SelectMany(i => i);

                context.AddItems(completions);

                // If Completion was triggered by this provider - use suggestion mode
                if (_triggeredByUs && context.SuggestionModeItem == null)
                {
                    context.SuggestionModeItem = CompletionItem.Create("");
                }
            }
            finally
            {
                _triggeredByUs = false;
            }
        }

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            if (Options == null || !Options.InvokeIntelliSenseAutomatically)
            {
                // Package not loaded yet (e.g. no solution opened)
                return false;
            }

            bool shouldTrigger = triggerCompletions.Any(c => c.ShouldTriggerCompletion(text, caretPosition, trigger, Options));

            _triggeredByUs = shouldTrigger;

            return shouldTrigger;
        }

        public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        {
            return CompletionCommitHelper.GetChangeAsync(document, item, cancellationToken);
        }

        public override async Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            return await CompletionItemHelper.GetDescriptionAsync(document, item, cancellationToken).ConfigureAwait(false)
                ?? await base.GetDescriptionAsync(document, item, cancellationToken).ConfigureAwait(false);
        }

        private async Task<bool> IsWatchWindowAsync(CompletionContext completionContext)
        {
            // Current line in watch window starts with ';'. Any other options to determine that?
            var sourceText = await completionContext.Document.GetTextAsync().ConfigureAwait(false);
            var currentLine = sourceText.Lines.GetLineFromPosition(completionContext.Position);
            return currentLine.ToString().StartsWith(";");
        }

        private bool IsRazorView(CompletionContext context)
        {
            return context.Document.Name != null && context.Document.Name.EndsWith(".cshtml");
        }
    }
}
