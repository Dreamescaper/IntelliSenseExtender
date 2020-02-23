using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IntelliSenseExtender.Context;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.IntelliSense.Context;
using IntelliSenseExtender.IntelliSense.Providers.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public class NewObjectCompletionProvider : ISimpleCompletionProvider, ITriggerCompletions
    {
        private static readonly Regex BracketRegex = new Regex(@"\w\($");

        public IEnumerable<CompletionItem>? GetCompletionItems(SyntaxContext syntaxContext, Options.Options options)
        {
            var type = syntaxContext.InferredInfo.Type;

            if (type == null)
                return null;

            var isNewKeywordPresent = syntaxContext.CurrentToken.Parent is ObjectCreationExpressionSyntax;

            // TODO make async
            var completions = GetTypeCompletionsAsync(syntaxContext, type, isNewKeywordPresent, options).Result;

            if (!isNewKeywordPresent)
            {
                completions = completions.Concat(GetSpecialCasesCompletions(type, syntaxContext));
                if (options.SuggestFactoryMethodsOnObjectCreation)
                {
                    completions = completions.Concat(GetStaticMethodsAndPropertiesCompletions(syntaxContext, type));
                }
            }

            return completions;
        }

        private async Task<IEnumerable<CompletionItem>> GetTypeCompletionsAsync(SyntaxContext syntaxContext, ITypeSymbol type, bool isNewKeywordPresent, Options.Options options)
        {
            var solution = syntaxContext.Document.Project.Solution;
            IEnumerable<INamedTypeSymbol> implementations = Array.Empty<INamedTypeSymbol>();

            if (type is INamedTypeSymbol namedType)
            {
                implementations = namedType.TypeKind switch
                {
                    TypeKind.Class => await SymbolFinder.FindDerivedClassesAsync(namedType, solution, cancellationToken: syntaxContext.CancellationToken),
                    TypeKind.Interface => (await SymbolFinder.FindImplementationsAsync(namedType, solution, cancellationToken: syntaxContext.CancellationToken)).OfType<INamedTypeSymbol>(),
                    _ => Array.Empty<INamedTypeSymbol>()
                };
            }

            if (type is INamedTypeSymbol namedTypeSymbol && (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct))
            {
                if (namedTypeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    if (namedTypeSymbol.TypeArguments[0] is INamedTypeSymbol typeArgumentNamedType)
                        implementations = implementations.Append(typeArgumentNamedType);
                }
                else
                {
                    implementations = implementations.Append(namedTypeSymbol);
                }
            }

            return FromAssignableTypes(implementations);

            IEnumerable<CompletionItem> FromAssignableTypes(IEnumerable<INamedTypeSymbol> assignableTypes)
            {
                foreach (var assignableType in assignableTypes)
                {
                    if (assignableType.IsBuiltInType()
                        || (assignableType.TypeKind != TypeKind.Class && assignableType.TypeKind != TypeKind.Struct)
                        || assignableType.IsAbstract
                        || !assignableType.InstanceConstructors.Any(con => con.DeclaredAccessibility == Accessibility.Public))
                    {
                        continue;
                    }

                    var completion = GetApplicableTypeCompletion(assignableType, syntaxContext, !isNewKeywordPresent, options);

                    if (completion != null)
                        yield return completion;
                }
            }
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
                        || textBeforeCaret.EndsWith(": ")
                        || textBeforeCaret.EndsWith(" = new ")
                        || textBeforeCaret.EndsWith("return ")
                        || BracketRegex.IsMatch(textBeforeCaret)))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsApplicable(SyntaxContext syntaxContext, Options.Options options)
        {
            return options.SuggestTypesOnObjectCreation
                && syntaxContext.InferredInfo.Type != null
                // do not suggest for comparisons
                && syntaxContext.InferredInfo.From != TypeInferredFrom.BinaryExpression
                // do not suggest for object
                && syntaxContext.InferredInfo.Type.SpecialType != SpecialType.System_Object
                // do not support enums and nullable enums
                && syntaxContext.InferredInfo.Type.TypeKind != TypeKind.Enum
                && !(syntaxContext.InferredInfo.Type is INamedTypeSymbol namedType
                    && namedType.Name == nameof(Nullable)
                    && namedType.TypeArguments.FirstOrDefault()?.TypeKind == TypeKind.Enum)
                // do not suggest for ref/out parameters
                && (syntaxContext.InferredInfo.ParameterSymbol == null
                    || syntaxContext.InferredInfo.ParameterSymbol.RefKind == RefKind.None);
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
                            && SymbolEqualityComparer.Default.Equals((symbol as IMethodSymbol)?.ReturnType, typeSymbol))
                        || SymbolEqualityComparer.Default.Equals((symbol as IPropertySymbol)?.Type, typeSymbol)));

            return factorySymbols.Select(symbol => CompletionItemHelper.CreateCompletionItem(symbol, syntaxContext,
                        Sorting.NewSuggestion_FactoryMethod,
                        MatchPriority.Preselect,
                        unimported: !syntaxContext.IsNamespaceImported(symbol.ContainingNamespace),
                        includeContainingClass: true));
        }

        private CompletionItem? GetApplicableTypeCompletion(ITypeSymbol suggestedType, SyntaxContext syntaxContext, bool newKeywordRequired, Options.Options options)
        {
            if (syntaxContext.InferredInfo.Type == null)
                return null;

            var assignableSymbol = syntaxContext.SemanticModel.Compilation.GetAssignableSymbol(suggestedType, syntaxContext.InferredInfo.Type);

            if (assignableSymbol == null
                || assignableSymbol.Name == nameof(Nullable))
            {
                return null;
            }

            var symbolName = assignableSymbol.Name;
            var inferredTypeName = syntaxContext.InferredInfo.Type.Name;
            bool unimported = !syntaxContext.IsNamespaceImported(assignableSymbol.ContainingNamespace);

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
                            const string listNamespace = "System.Collections.Generic";

                            var unimported = !syntaxContext.IsNamespaceImported(listNamespace);
                            var displayName = typeParameter.ToMinimalDisplayString(syntaxContext.SemanticModel, syntaxContext.Position);
                            var completion = CompletionItemHelper.CreateCompletionItem(
                                    $"new List<{displayName}> {{}}",
                                    Sorting.NewSuggestion_CollectionInitializer,
                                    namespaceToImport: unimported ? listNamespace : null,
                                    newPositionOffset: -1);
                            return new[] { completion };
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
    }
}
