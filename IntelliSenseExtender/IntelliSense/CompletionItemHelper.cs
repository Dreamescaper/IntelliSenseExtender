﻿using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.ExposedInternals;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.IntelliSense.Context;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Tags;

namespace IntelliSenseExtender.IntelliSense
{
    public static class CompletionItemHelper
    {
        private static readonly SymbolDisplayFormat BoundGenericFormat = SymbolDisplayFormat.CSharpShortErrorMessageFormat;

        public static (string displayText, string? suffixText) GetDisplayInsertText(ISymbol symbol, SyntaxContext context,
            string? alias, bool includeContainingType, bool newCreation, bool showParenthesisForNewCreations)
        {
            const string AttributeSuffix = nameof(Attribute);

            string displayText;
            string? suffixText = null;

            string symbolName = alias ?? symbol.GetAccessibleName(context);
            if (context.IsAttributeContext && symbolName.EndsWith(AttributeSuffix))
            {
                displayText = symbolName.Substring(0, symbolName.Length - AttributeSuffix.Length);
            }
            else if (alias == null && symbol is INamedTypeSymbol typeSymbol && typeSymbol.Arity > 0)
            {
                //If generic type is unbound - do not show generic arguments
                if (Enumerable.SequenceEqual(typeSymbol.TypeArguments, typeSymbol.TypeParameters, SymbolEqualityComparer.Default))
                {
                    displayText = symbolName;
                    suffixText = "<>";
                }
                else
                {
                    displayText = symbol.ToDisplayString(BoundGenericFormat);
                }
            }
            else if (symbol is IMethodSymbol methodSymbol && methodSymbol.Arity > 0)
            {
                displayText = symbolName;
                suffixText = "<>";
            }
            else
            {
                displayText = symbolName;
            }

            if (includeContainingType)
            {
                var containingType = symbol.ContainingType;
                var typeName = containingType.Name;
                displayText = $"{typeName}.{displayText}";
            }

            if (newCreation)
            {
                displayText = $"new {displayText}";
                if (showParenthesisForNewCreations)
                {
                    if (suffixText != null)
                    {
                        displayText += suffixText;
                        suffixText = null;
                    }

                    displayText += "()";
                }
            }

            return (displayText, suffixText);
        }

        public static async Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            if (!item.Properties.TryGetValue(CompletionItemProperties.FullSymbolName, out var fullyQualifiedName))
                return null;

            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);

            if (semanticModel == null)
                return null;

            var globalNamespace = semanticModel.Compilation.GlobalNamespace;
            var symbol = SymbolNavigator.FindSymbolByFullName(globalNamespace, fullyQualifiedName);

            if (symbol == null)
                return null;

            string symbolKey = SymbolCompletionItem.EncodeSymbol(symbol);
            item = item.AddProperty(CompletionItemProperties.Symbols, symbolKey);

            var descriptionTask = SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken).ConfigureAwait(false);

            bool unimported = item.Properties.ContainsKey(CompletionItemProperties.NamespaceToImport);

            var description = await descriptionTask;

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
            int sortingPriority = Sorting.Default, int matchPriority = -1, int newPositionOffset = 0,
            bool unimported = true,
            bool newCreationSyntax = false, bool showParenthesisForNewCreations = false,
            bool includeContainingClass = false)
        {
            var accessabilityTag = GetAccessabilityTag(symbol);
            var kindTag = GetSymbolKindTag(symbol);
            var tags = ImmutableArray.Create(kindTag, accessabilityTag);

            var rules = CompletionItemRules.Create(
                    matchPriority: matchPriority
                );

            var nsName = symbol.GetNamespace();
            var fullSymbolName = symbol.GetFullyQualifiedName(nsName);

            string? aliasName = null;
            if (symbol is ITypeSymbol typeSymbol && context.Aliases.TryGetValue(typeSymbol, out aliasName))
                unimported = false;

            if (symbol.ContainingSymbol is ITypeSymbol parentTypeSymbol && context.StaticImports.Contains(parentTypeSymbol))
                unimported = false;

            (string displayText, string? suffixText) = GetDisplayInsertText(symbol, context,
                aliasName, includeContainingClass, newCreationSyntax, showParenthesisForNewCreations);
            var props = ImmutableDictionary.CreateBuilder<string, string>();
            props.Add(CompletionItemProperties.FullSymbolName, fullSymbolName);

            if (unimported)
                props.Add(CompletionItemProperties.NamespaceToImport, nsName);

            if (newPositionOffset != 0)
                props.Add(CompletionItemProperties.NewPositionOffset, newPositionOffset.ToString());

            var sortText = GetSortText(symbol.GetAccessibleName(context), nsName, sortingPriority, unimported);

            var inlineDescription = unimported ? nsName : null;

            return CompletionItem.Create(
                displayText: displayText,
                displayTextSuffix: suffixText,
                inlineDescription: inlineDescription,
                sortText: sortText,
                properties: props.ToImmutable(),
                tags: tags,
                rules: rules);
        }

        public static CompletionItem CreateCompletionItem(string itemText, int sortingPriority,
            ImmutableArray<string> tags = default,
            string? namespaceToImport = null,
            int newPositionOffset = 0)
        {
            var rules = CompletionItemRules.Create(
                    matchPriority: MatchPriority.Preselect
                );

            var sortText = GetSortText(itemText, string.Empty, sortingPriority, false);
            var properties = ImmutableDictionary<string, string>.Empty;

            if (newPositionOffset != 0)
                properties = properties.Add(CompletionItemProperties.NewPositionOffset, newPositionOffset.ToString());

            string? inlineDescription = null;
            if (namespaceToImport != null)
            {
                properties = properties
                    .Add(CompletionItemProperties.NamespaceToImport, namespaceToImport);

                inlineDescription = namespaceToImport;
            }

            return CompletionItem.Create(
                displayText: itemText,
                sortText: sortText,
                inlineDescription: inlineDescription,
                tags: tags,
                properties: properties,
                rules: rules);
        }

        private static string GetSortText(string symbolName, string namespaceName, int sortingPriority, bool unimported)
        {
            var prefix = sortingPriority switch
            {
                Sorting.Default => string.Empty,
                Sorting.Last => "~",
                _ => "!" + sortingPriority + "_",
            };

            // Add namespace to the end so items with same name would be displayed
            // (only for unimported values)
            var suffix = unimported ? " " + namespaceName : string.Empty;
            return prefix + symbolName + suffix;
        }

        private static string GetAccessabilityTag(ISymbol symbol)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    return WellKnownTags.Public;
                case Accessibility.Private:
                    return WellKnownTags.Private;
                case Accessibility.Internal:
                    return WellKnownTags.Internal;
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                case Accessibility.ProtectedAndInternal:
                    return WellKnownTags.Protected;
                case Accessibility.NotApplicable:
                    return string.Empty;
                default:
                    throw new ArgumentException($"Accessibility '{symbol.DeclaredAccessibility}' is not supported!");
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
                                return WellKnownTags.Class;
                            case TypeKind.Interface:
                                return WellKnownTags.Interface;
                            case TypeKind.Enum:
                                return WellKnownTags.Enum;
                            case TypeKind.Struct:
                                return WellKnownTags.Structure;
                            case TypeKind.Delegate:
                                return WellKnownTags.Delegate;
                        }
                    }
                    break;
                case SymbolKind.Method when ((IMethodSymbol)symbol).IsExtensionMethod:
                    return WellKnownTags.ExtensionMethod;
                case SymbolKind.Method:
                    return WellKnownTags.Method;
                case SymbolKind.Local:
                    return WellKnownTags.Local;
                case SymbolKind.Parameter:
                    return WellKnownTags.Parameter;
                case SymbolKind.Field:
                    return WellKnownTags.Field;
                case SymbolKind.Property:
                    return WellKnownTags.Property;
            }

            return string.Empty;
        }
    }
}
