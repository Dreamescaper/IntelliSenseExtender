using System;
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

        public static CompletionItem CreateCompletionItem(ISymbol symbol, SyntaxContext context)
        {
            var accessabilityTag = GetAccessabilityTag(symbol);
            var kindTag = GetSymbolKindTag(symbol);
            var tags = ImmutableArray.Create(kindTag, accessabilityTag);

            // Make those items less prioritized
            var rules = CompletionItemRules.Create(
                    matchPriority: -1
                );

            // In original Roslyn SymbolCompletionProvider SymbolsProperty is set
            // for all items. However, for huge items quantity
            // encoding has significant performance impact. We will put it in GetDescriptionAsync.

            var fullSymbolName = symbol.GetFullyQualifiedName();

            var props = ImmutableDictionary<string, string>.Empty
                .Add(CompletionItemProperties.ContextPosition, context.Position.ToString())
                .Add(CompletionItemProperties.SymbolName, symbol.Name)
                .Add(CompletionItemProperties.FullSymbolName, fullSymbolName);

            // Add namespace to the end so items with same name would be displayed
            var sortText = symbol.Name + " " + fullSymbolName;

            return CompletionItem.Create(
                displayText: GetDisplayText(symbol, context),
                sortText: sortText,
                properties: props,
                rules: rules,
                tags: tags);
        }

        private static string GetAccessabilityTag(ISymbol symbol)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return CompletionTags.Public;
                case Accessibility.Private:
                    return CompletionTags.Private;
                case Accessibility.Internal:
                    return CompletionTags.Internal;
                case Accessibility.Protected:
                    return CompletionTags.Protected;
                case Accessibility.NotApplicable:
                    return string.Empty;
                default:
                    throw new ArgumentException($"Accessability '{symbol.DeclaredAccessibility}' is not supported!");
            }
        }

        private static string GetSymbolKindTag(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                    return CompletionTags.Class;
                case SymbolKind.Method:
                    if (symbol is IMethodSymbol methodSymbol && methodSymbol.IsExtensionMethod)
                    {
                        return CompletionTags.ExtensionMethod;
                    }
                    return CompletionTags.Method;
                default:
                    return string.Empty;
            }
        }
    }
}
