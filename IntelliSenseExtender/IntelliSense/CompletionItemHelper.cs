using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.ExposedInternals;
using IntelliSenseExtender.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.IntelliSense
{
    public static class CompletionItemHelper
    {
        public static string GetDisplayText(ISymbol symbol, SyntaxContext context)
        {
            const string AttributeSuffix = "Attribute";

            var symbolText = symbol.Name;
            if (context.IsAttributeContext
                && symbolText.EndsWith(AttributeSuffix))
            {
                symbolText = symbolText.Substring(0, symbolText.Length - AttributeSuffix.Length);
            }

            return symbolText;
        }

        public static async Task<CompletionDescription> GetUnimportedDescriptionAsync(Document document, CompletionItem item, ISymbol symbol, CancellationToken cancellationToken)
        {
            string symbolKey = SymbolCompletionItem.EncodeSymbol(symbol);
            item = item.AddProperty(CompletionItemProperties.Symbols, symbolKey);

            var description = await SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken).ConfigureAwait(false);

            // Adding 'unimported' text to beginning
            var unimportedTextParts = ImmutableArray<TaggedText>.Empty
                .Add(new TaggedText(TextTags.Text, "(unimported)"))
                .Add(new TaggedText(TextTags.Space, " "))
                .AddRange(description.TaggedParts);

            return description.WithTaggedParts(unimportedTextParts);
        }

        public static CompletionItem CreateCompletionItem(ISymbol typeSymbol, SyntaxContext context)
        {
            var accessabilityTag = typeSymbol.DeclaredAccessibility == Accessibility.Public
                ? CompletionTags.Public
                : CompletionTags.Private;
            var tags = ImmutableArray.Create(CompletionTags.Class, accessabilityTag);

            // Make those items less prioritized
            var rules = CompletionItemRules.Create(
                    matchPriority: -1
                );


            // In original Roslyn SymbolCompletionProvider SymbolsProperty is set
            // for all items. However, for huge items quantity
            // encoding has significant performance impact. We will put it in GetDescriptionAsync.

            var fullSymbolName = typeSymbol.GetFullyQualifiedName();

            var props = ImmutableDictionary<string, string>.Empty
                .Add(CompletionItemProperties.ContextPosition, context.Position.ToString())
                .Add(CompletionItemProperties.SymbolName, typeSymbol.Name)
                .Add(CompletionItemProperties.FullSymbolName, fullSymbolName);

            // Add namespace to the end so items with same name would be displayed
            var sortText = typeSymbol.Name + " " + fullSymbolName;

            return CompletionItem.Create(
                displayText: GetDisplayText(typeSymbol, context),
                sortText: sortText,
                properties: props,
                rules: rules,
                tags: tags);
        }
    }
}
