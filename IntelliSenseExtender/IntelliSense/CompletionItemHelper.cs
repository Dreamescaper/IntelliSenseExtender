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
        public static (string displayText, string insertText) GetDisplayInsertText(ISymbol symbol, SyntaxContext context, string @namespace)
        {
            const string AttributeSuffix = "Attribute";

            string displayText = symbol.Name;
            string insertText = displayText;

            if (context.IsAttributeContext
                && displayText.EndsWith(AttributeSuffix))
            {
                displayText = displayText.Substring(0, displayText.Length - AttributeSuffix.Length);
                insertText = displayText;
            }
            else if ((symbol is IMethodSymbol methodSymbol && methodSymbol.Arity > 0)
                || (symbol is INamedTypeSymbol typeSymbol && typeSymbol.Arity > 0))
            {
                displayText += "<>";
            }

            displayText += $"  ({@namespace})";

            return (displayText, insertText);
        }

        public static async Task<CompletionDescription> GetUnimportedDescriptionAsync(Document document, CompletionItem item, ISymbol symbol, CancellationToken cancellationToken)
        {
            string symbolKey = SymbolCompletionItem.EncodeSymbol(symbol);
            item = item.AddProperty(CompletionItemProperties.Symbols, symbolKey);

            var description = await SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken).ConfigureAwait(false);

            // Adding 'unimported' text to beginning
            var unimportedTextParts = ImmutableArray.CreateBuilder<TaggedText>(2 + description.TaggedParts.Length);
            unimportedTextParts.Add(new TaggedText(TextTags.Text, "(unimported)"));
            unimportedTextParts.Add(new TaggedText(TextTags.Space, " "));
            unimportedTextParts.AddRange(description.TaggedParts);

            return description.WithTaggedParts(unimportedTextParts.MoveToImmutable());
        }

        public static CompletionItem CreateCompletionItem(ISymbol symbol, SyntaxContext context, bool sortLast)
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
            var nsName = symbol.GetNamespace();

            (string displayText, string insertText) = GetDisplayInsertText(symbol, context, nsName);

            var props = ImmutableDictionary.CreateBuilder(StringComparer.Ordinal, StringComparer.Ordinal);

            props.Add(CompletionItemProperties.ContextPosition, context.Position.ToString());
            props.Add(CompletionItemProperties.SymbolName, symbol.Name);
            props.Add(CompletionItemProperties.FullSymbolName, fullSymbolName);
            props.Add(CompletionItemProperties.Namespace, nsName);
            props.Add(CompletionItemProperties.InsertText, insertText);

            // Add namespace to the end so items with same name would be displayed
            var sortText = GetSortText(symbol.Name, nsName, sortLast);

            return CompletionItem.Create(
                displayText: displayText,
                sortText: sortText,
                filterText: insertText,
                properties: props.ToImmutable(),
                rules: rules,
                tags: tags);
        }

        private static string GetSortText(string symbolName, string nsName, bool sortLast)
        {
            string prefix = sortLast ? "~" : string.Empty;
            return prefix + symbolName + " " + nsName;
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
                    if (symbol is INamedTypeSymbol typeSymbol)
                    {
                        switch (typeSymbol.TypeKind)
                        {
                            case TypeKind.Class:
                                return CompletionTags.Class;
                            case TypeKind.Interface:
                                return CompletionTags.Interface;
                            case TypeKind.Enum:
                                return CompletionTags.Enum;
                            case TypeKind.Struct:
                                return CompletionTags.Structure;
                            case TypeKind.Delegate:
                                return CompletionTags.Delegate;
                        }
                    }
                    break;
                case SymbolKind.Method:
                    if (symbol is IMethodSymbol methodSymbol && methodSymbol.IsExtensionMethod)
                    {
                        return CompletionTags.ExtensionMethod;
                    }
                    return CompletionTags.Method;
            }

            return string.Empty;
        }
    }
}
