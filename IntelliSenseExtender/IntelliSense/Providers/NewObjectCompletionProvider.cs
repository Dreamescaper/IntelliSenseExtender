using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    [ExportCompletionProvider("Object creation provider", LanguageNames.CSharp)]
    public class NewObjectCompletionProvider : AbstractCompletionProvider
    {
        public NewObjectCompletionProvider() : base()
        {
        }

        public NewObjectCompletionProvider(IOptionsProvider optionsProvider) : base(optionsProvider)
        {
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            if (Options.SuggestTypesOnObjectCreation || Options.SuggestFactoryMethodsOnObjectCreation)
            {
                var syntaxContext = await SyntaxContext.Create(context.Document, context.Position, context.CancellationToken).ConfigureAwait(false);

                bool newKeywordRequired = true;
                var currentSyntaxNode = syntaxContext.CurrentToken.Parent;
                if (currentSyntaxNode is ObjectCreationExpressionSyntax)
                {
                    //if we already have new keyword - we don't need that
                    newKeywordRequired = false;
                }

                if (TryGetTypeSymbol(syntaxContext, out ITypeSymbol typeSymbol))
                {
                    if (!typeSymbol.IsBuiltInType())
                    {
                        if (Options.SuggestTypesOnObjectCreation)
                        {
                            var typeCompletionItems = GetApplicableTypesCompletions(syntaxContext, typeSymbol, newKeywordRequired);
                            context.AddItems(typeCompletionItems);
                        }
                        if (Options.SuggestFactoryMethodsOnObjectCreation)
                        {
                            var factoryMethodsCompletions = GetFactoryMethodsAndPropertiesCompletions(syntaxContext, typeSymbol);
                            context.AddItems(factoryMethodsCompletions);
                        }
                    }

                    context.AddItems(GetSpecialCasesCompletions(typeSymbol, syntaxContext));
                    ReplaceNewKeywordSuggestion(context);
                }
            }
        }

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            if (Options.SuggestTypesOnObjectCreation || Options.SuggestFactoryMethodsOnObjectCreation)
            {
                var sourceString = text.ToString();

                //trigger completion automatically when assigning values
                var textBeforeCaret = sourceString.Substring(0, caretPosition);
                if (trigger.Kind == CompletionTriggerKind.Insertion
                    && (textBeforeCaret.EndsWith(" = ") || textBeforeCaret.EndsWith("new ")))
                {
                    return true;
                }
            }

            return base.ShouldTriggerCompletion(text, caretPosition, trigger, options);
        }

        private IEnumerable<CompletionItem> GetFactoryMethodsAndPropertiesCompletions(SyntaxContext syntaxContext, ITypeSymbol typeSymbol)
        {
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

        private IEnumerable<CompletionItem> GetApplicableTypesCompletions(SyntaxContext syntaxContext, ITypeSymbol typeSymbol, bool newKeywordRequired)
        {
            var symbols = GetAllTypes(syntaxContext, syntaxContext.CancellationToken)
                .Select(type => syntaxContext.SemanticModel.Compilation.GetAssignableSymbol(type, typeSymbol))
                .Where(type => type != null && !type.IsBuiltInType());

            var completionItems = symbols.Select(symbol =>
                {
                    var symbolName = symbol.Name;
                    var typeSymbolName = typeSymbol.Name;
                    bool unimported = !syntaxContext.ImportedNamespaces.Contains(symbol.GetNamespace());

                    int priority;
                    if (symbolName == typeSymbolName || "I" + symbolName == typeSymbolName)
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

                    var item = CompletionItemHelper.CreateCompletionItem(symbol, syntaxContext,
                        priority, MatchPriority.Preselect,
                        newPositionOffset: 0,
                        unimported: unimported,
                        newCreationSyntax: newKeywordRequired);
                    return item;
                });

            return completionItems;
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
                var typeName = namedTypeSymbol.Name;
                switch (typeName)
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
                                .WithTags(ImmutableArray.Create(CompletionTags.Keyword)),
                             CompletionItemHelper.CreateCompletionItem("false", Sorting.NewSuggestion_Literal)
                                .WithTags(ImmutableArray.Create(CompletionTags.Keyword))
                        };
                }
            }

            return Enumerable.Empty<CompletionItem>();
        }

        protected override bool FilterType(INamedTypeSymbol type, SyntaxContext syntaxContext)
        {
            return
                (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct)
                && !type.IsAbstract
                && type.InstanceConstructors.Any(con => con.DeclaredAccessibility == Accessibility.Public);
        }

        private bool TryGetTypeSymbol(SyntaxContext syntaxContext, out ITypeSymbol typeSymbol)
        {
            SyntaxNode currentSyntaxNode = syntaxContext.CurrentToken.Parent;

            typeSymbol = null;

            // If new keyword is already present, we need to work with parent node
            if (currentSyntaxNode is ObjectCreationExpressionSyntax)
            {
                currentSyntaxNode = currentSyntaxNode.Parent;
            }

            if (currentSyntaxNode?.Parent?.Parent is VariableDeclarationSyntax varDeclarationSyntax
                && !varDeclarationSyntax.Type.IsVar)
            {
                var typeInfo = syntaxContext.SemanticModel.GetTypeInfo(varDeclarationSyntax.Type);
                typeSymbol = typeInfo.Type;
            }
            else if (currentSyntaxNode is AssignmentExpressionSyntax assigmentExpressionSyntax)
            {
                var typeInfo = syntaxContext.SemanticModel.GetTypeInfo(assigmentExpressionSyntax.Left);
                typeSymbol = typeInfo.Type;
            }
            else if (currentSyntaxNode is ArgumentSyntax argumentSyntax)
            {
                typeSymbol = syntaxContext.SemanticModel.GetArgumentTypeSymbol(argumentSyntax);
            }
            else if (currentSyntaxNode is ArgumentListSyntax argumentListSyntax)
            {
                int parameterIndex = argumentListSyntax.ChildTokens()
                    .Where(token => token.ValueText == ",")
                    .ToList().IndexOf(syntaxContext.CurrentToken) + 1;
                var parameters = syntaxContext.SemanticModel.GetParameters(argumentListSyntax);

                typeSymbol = parameters?.ElementAtOrDefault(parameterIndex)?.Type;
            }

            return typeSymbol != null;
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
                tags: ImmutableArray.Create(CompletionTags.Keyword));
            context.AddItem(newSuggestion);
        }
    }
}
