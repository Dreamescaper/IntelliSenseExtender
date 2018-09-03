using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using IntelliSenseExtender.Context;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.IntelliSense.Providers.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public class NewObjectCompletionProvider : ISimpleCompletionProvider, ITypeCompletionProvider, ITriggerCompletions
    {
        private static readonly Regex BracketRegex = new Regex(@"\w\($");
        private static readonly Regex AttributeRegex = new Regex(@"\[\w+\(([^\] ]+(, )?)*$");

        public IEnumerable<CompletionItem> GetCompletionItems(SyntaxContext syntaxContext, Options.Options options)
        {
            if (syntaxContext.InferredType != null)
            {
                var completions = GetSpecialCasesCompletions(syntaxContext.InferredType, syntaxContext);
                if (options.SuggestFactoryMethodsOnObjectCreation)
                {
                    completions = completions.Concat(GetStaticMethodsAndPropertiesCompletions(syntaxContext, syntaxContext.InferredType));
                }

                return completions;
            }

            return null;
        }

        public IEnumerable<CompletionItem> GetCompletionItemsForType(INamedTypeSymbol typeSymbol, SyntaxContext syntaxContext, Options.Options options)
        {
            if (typeSymbol.IsBuiltInType()
                || (typeSymbol.TypeKind != TypeKind.Class && typeSymbol.TypeKind != TypeKind.Struct)
                || typeSymbol.IsAbstract
                || !typeSymbol.InstanceConstructors.Any(con => con.DeclaredAccessibility == Accessibility.Public))
            {
                return null;
            }

            bool newKeywordRequired = true;
            var currentSyntaxNode = syntaxContext.CurrentToken.Parent;
            if (currentSyntaxNode is ObjectCreationExpressionSyntax)
            {
                //if we already have new keyword - we don't need that
                newKeywordRequired = false;
            }

            var applicableCompletion = GetApplicableTypeCompletion(typeSymbol, syntaxContext, newKeywordRequired, options);
            return applicableCompletion == null ? null : new[] { applicableCompletion };
        }

        public bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, Options.Options options)
        {
            if (options.SuggestTypesOnObjectCreation || options.SuggestFactoryMethodsOnObjectCreation)
            {
                var currentLine = text.Lines.GetLineFromPosition(caretPosition);

                //trigger completion automatically when assigning values
                var textBeforeCaret = currentLine.ToString().Substring(0, caretPosition - currentLine.Start);
                if (trigger.Kind == CompletionTriggerKind.Insertion
                    && (textBeforeCaret.EndsWith(" = ")
                        || textBeforeCaret.EndsWith(" = new ")
                        || textBeforeCaret.EndsWith("return ")
                        || BracketRegex.IsMatch(textBeforeCaret))
                    && !AttributeRegex.IsMatch(textBeforeCaret))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsApplicable(SyntaxContext syntaxContext, Options.Options options)
        {
            return options.SuggestTypesOnObjectCreation
                && syntaxContext.InferredType != null
                // do not support enums and nullable enums
                && syntaxContext.InferredType.TypeKind != TypeKind.Enum
                && !(syntaxContext.InferredType is INamedTypeSymbol namedType
                    && namedType.Name == "Nullable"
                    && namedType.TypeArguments.FirstOrDefault()?.TypeKind == TypeKind.Enum);
        }

        private IEnumerable<CompletionItem> GetStaticMethodsAndPropertiesCompletions(SyntaxContext syntaxContext, ITypeSymbol typeSymbol)
        {
            // Do not suggest completions for static methods or properties for build-in types.
            // There's too much of them, and they only pollute IntelliSence
            if (typeSymbol.IsBuiltInType())
            {
                return Enumerable.Empty<CompletionItem>();
            }

            var factorySymbols = typeSymbol.GetMembers()
                .Where(symbol => symbol.IsStatic
                    && symbol.DeclaredAccessibility == Accessibility.Public
                    && (((symbol as IMethodSymbol)?.MethodKind == MethodKind.Ordinary
                            && (symbol as IMethodSymbol)?.ReturnType == typeSymbol)
                        || (symbol as IPropertySymbol)?.Type == typeSymbol));

            return factorySymbols.Select(symbol => CompletionItemHelper.CreateCompletionItem(symbol, syntaxContext,
                        Sorting.NewSuggestion_FactoryMethod,
                        MatchPriority.Preselect,
                        unimported: !syntaxContext.ImportedNamespaces.Contains(symbol.GetNamespace()),
                        includeContainingClass: true));
        }

        private CompletionItem GetApplicableTypeCompletion(ITypeSymbol suggestedType, SyntaxContext syntaxContext, bool newKeywordRequired, Options.Options options)
        {
            var assignableSymbol = syntaxContext.SemanticModel.Compilation.GetAssignableSymbol(suggestedType, syntaxContext.InferredType);

            if (assignableSymbol != null)
            {
                var symbolName = assignableSymbol.Name;
                var inferredTypeName = syntaxContext.InferredType.Name;
                bool unimported = !syntaxContext.ImportedNamespaces.Contains(assignableSymbol.GetNamespace());

                int priority;
                if (symbolName == inferredTypeName || "I" + symbolName == inferredTypeName)
                {
                    priority = Sorting.NewSuggestion_MatchingName;
                }
                else if (!unimported)
                {
                    priority = Sorting.NewSuggestion_Default;
                }
                else
                {
                    priority = Sorting.NewSuggestion_Unimported;
                }

                return CompletionItemHelper.CreateCompletionItem(assignableSymbol, syntaxContext,
                    priority, MatchPriority.Preselect,
                    newPositionOffset: 0,
                    unimported: unimported,
                    newCreationSyntax: newKeywordRequired,
                    showParenthesisForNewCreations: options.AddParethesisForNewSuggestions);
            }
            return null;
        }

        /// <summary>
        /// Return suggestions for special cases, such as array or collection initializers
        /// </summary>
        private IEnumerable<CompletionItem> GetSpecialCasesCompletions(ITypeSymbol typeSymbol, SyntaxContext syntaxContext)
        {
            if (typeSymbol is IArrayTypeSymbol)
            {
                return new[] { CompletionItemHelper.CreateCompletionItem("new [] {}", Sorting.NewSuggestion_CollectionInitializer, newPositionOffset: -1) };
            }

            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                switch (namedTypeSymbol.Name)
                {
                    case "List":
                    case "IList":
                        var typeParameter = namedTypeSymbol.TypeArguments.FirstOrDefault();
                        if (typeParameter != null)
                        {
                            var displayName = typeParameter.ToMinimalDisplayString(syntaxContext.SemanticModel, syntaxContext.Position);
                            return new[] { CompletionItemHelper.CreateCompletionItem($"new List<{displayName}> {{}}", Sorting.NewSuggestion_CollectionInitializer, newPositionOffset: -1) };
                        }
                        break;
                    case "Boolean":
                        return new[]
                        {
                             CompletionItemHelper.CreateCompletionItem("true", Sorting.NewSuggestion_Literal)
                                .WithTags(ImmutableArray.Create(WellKnownTags.Keyword)),
                             CompletionItemHelper.CreateCompletionItem("false", Sorting.NewSuggestion_Literal)
                                .WithTags(ImmutableArray.Create(WellKnownTags.Keyword))
                        };
                }
            }

            return Enumerable.Empty<CompletionItem>();
        }

        /// <summary>
        /// Default new keyword completion item is auto-committed on space button, which 
        /// is not desired for new object creation suggestions.
        /// </summary>
        /// <param name="context">CompletionContext to replace keyword in</param>
        private void ReplaceNewKeywordSuggestion(CompletionContext context)
        {
            //Add two spaces to filter text so it wouldn't be automatically selected when 'new' is typed
            var newSuggestion = CompletionItem.Create(
                displayText: "new",
                filterText: "new  ",
                tags: ImmutableArray.Create(WellKnownTags.Keyword));
            context.AddItem(newSuggestion);
        }
    }
}
