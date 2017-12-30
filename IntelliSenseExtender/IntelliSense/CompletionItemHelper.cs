using System;
using System.Collections.Immutable;
using System.Linq;
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
        public static (string displayText, string insertText) GetDisplayInsertText(ISymbol symbol, SyntaxContext context, string @namespace, bool unimported, bool includeContainingType, bool newCreation)
        {
            const string AttributeSuffix = "Attribute";

            string displayText;
            string insertText;

            var symbolName = symbol.Name;

            if (context.IsAttributeContext && symbolName.EndsWith(AttributeSuffix))
            {
                displayText = symbolName.Substring(0, symbolName.Length - AttributeSuffix.Length);
                insertText = displayText;
            }
            else if (symbol is INamedTypeSymbol typeSymbol && typeSymbol.Arity > 0)
            {
                //If generic type is unbound - do not show generic arguments
                if (Enumerable.SequenceEqual(typeSymbol.TypeArguments, typeSymbol.TypeParameters))
                {
                    displayText = symbolName + "<>";
                    insertText = symbolName;
                }
                else
                {
                    displayText = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                    insertText = displayText;
                }
            }
            else if (symbol is IMethodSymbol methodSymbol && methodSymbol.Arity > 0)
            {
                displayText = symbolName + "<>";
                insertText = symbolName;
            }
            else
            {
                displayText = symbolName;
                insertText = symbolName;
            }

            if (includeContainingType)
            {
                var containingType = symbol.ContainingType;
                var typeName = containingType.Name;
                displayText = $"{typeName}.{displayText}";
                insertText = displayText;
            }

            if (newCreation)
            {
                displayText = $"new {displayText}()";
                insertText = displayText;
            }

            if (unimported)
            {
                displayText += $"  ({@namespace})";
            }

            return (displayText, insertText);
        }

        public static async Task<CompletionDescription> GetUnimportedDescriptionAsync(Document document, CompletionItem item, ISymbol symbol, CancellationToken cancellationToken)
        {
            string symbolKey = SymbolCompletionItem.EncodeSymbol(symbol);
            item = item.AddProperty(CompletionItemProperties.Symbols, symbolKey);

            var description = await SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken).ConfigureAwait(false);

            bool unimported = item.Properties.TryGetValue(CompletionItemProperties.Unimported, out string unimportedString)
                && bool.Parse(unimportedString);

            if (unimported)
            {
                // Adding 'unimported' text to beginning
                var unimportedTextParts = ImmutableArray<TaggedText>.Empty
                    .Add(new TaggedText(TextTags.Text, "(unimported)"))
                    .Add(new TaggedText(TextTags.Space, " "))
                    .AddRange(description.TaggedParts);

                description = description.WithTaggedParts(unimportedTextParts);
            }

            return description;
        }

        public static CompletionItem CreateCompletionItem(ISymbol symbol, SyntaxContext context,
            int sortingPriority = Sorting.Default, int matchPriority = -12, int newPositionOffset = 0,
            bool unimported = true, bool newCreationSyntax = false, bool includeContainingClass = false)
        {
            var accessabilityTag = GetAccessabilityTag(symbol);
            var kindTag = GetSymbolKindTag(symbol);
            var tags = ImmutableArray.Create(kindTag, accessabilityTag);

            var rules = CompletionItemRules.Create(
                    matchPriority: matchPriority
                );

            // In original Roslyn SymbolCompletionProvider SymbolsProperty is set
            // for all items. However, for huge items quantity
            // encoding has significant performance impact. We will put it in GetDescriptionAsync.

            var fullSymbolName = symbol.GetFullyQualifiedName();
            var nsName = symbol.GetNamespace();

            (string displayText, string insertText) = GetDisplayInsertText(symbol, context, nsName, unimported, includeContainingClass, newCreationSyntax);
            var props = ImmutableDictionary.CreateBuilder<string, string>();
            props.Add(CompletionItemProperties.ContextPosition, context.Position.ToString());
            props.Add(CompletionItemProperties.SymbolName, symbol.Name);
            props.Add(CompletionItemProperties.FullSymbolName, fullSymbolName);
            props.Add(CompletionItemProperties.Namespace, nsName);
            props.Add(CompletionItemProperties.InsertText, insertText);
            props.Add(CompletionItemProperties.NewPositionOffset, newPositionOffset.ToString());
            props.Add(CompletionItemProperties.Unimported, unimported.ToString());

            // Add namespace to the end so items with same name would be displayed
            var sortText = GetSortText(symbol.Name, nsName, sortingPriority);

            return CompletionItem.Create(
                displayText: displayText,
                filterText: insertText,
                sortText: sortText,
                properties: props.ToImmutable(),
                tags: tags,
                rules: rules);
        }

        public static CompletionItem CreateCompletionItem(string itemText, int sortingPriority, int newPositionOffset = 0)
        {
            var rules = CompletionItemRules.Create(
                    matchPriority: MatchPriority.Preselect
                );

            var sortText = GetSortText(itemText, string.Empty, sortingPriority);
            var properties = ImmutableDictionary<string, string>.Empty
                .Add(CompletionItemProperties.NewPositionOffset, newPositionOffset.ToString());

            return CompletionItem.Create(
                    displayText: itemText,
                    sortText: sortText,
                    properties: properties,
                    rules: rules
                );
        }

        private static string GetSortText(string symbolName, string namespaceName, int sortingPriority)
        {
            string prefix;
            switch (sortingPriority)
            {
                case Sorting.Default:
                    prefix = string.Empty;
                    break;
                case Sorting.Last:
                    prefix = "~_";
                    break;
                default:
                    prefix = "!" + sortingPriority + "_";
                    break;
            }

            // Use '!' as separator between nsName and symbolName, so that shorter name item 
            // would be shown higher than longer (e.g. ClassName < ClassNameWithLongName)
            return $"{prefix}{symbolName}!{namespaceName}";
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

    public static class Sorting
    {
        public const int Default = -1;
        public const int Last = -2;
        public static int WithPriority(int i) => i;
    }
}
