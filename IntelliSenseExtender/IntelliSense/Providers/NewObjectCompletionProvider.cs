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
            if (Options.SuggestOnObjectCreation)
            {
                var syntaxContext = await SyntaxContext.Create(context.Document, context.Position, context.CancellationToken);

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
                        var symbols = GetAllTypes(syntaxContext)
                            .Select(type => syntaxContext.SemanticModel.Compilation.GetAssignableSymbol(type, typeSymbol))
                            .Where(type => type != null && !type.IsBuiltInType())
                            .ToList();

                        var completionItems = symbols.Select(symbol =>
                            {
                                var symbolName = symbol.Name;
                                var typeSymbolName = typeSymbol.Name;
                                var priority = symbolName == typeSymbolName || "I" + symbolName == typeSymbolName
                                    ? Sorting.WithPriority(4)
                                    : Sorting.WithPriority(5);

                                bool unimported = !syntaxContext.ImportedNamespaces.Contains(symbol.GetNamespace());

                                return CompletionItemHelper.CreateCompletionItem(symbol, syntaxContext,
                                    priority, MatchPriority.Preselect,
                                    newPositionOffset: 0,
                                    unimported: unimported,
                                    newCreationSyntax: newKeywordRequired);
                            })
                            .ToList();

                        context.AddItems(completionItems);
                    }

                    context.AddItems(GetSpecialCasesCompletions(typeSymbol, syntaxContext));
                    ReplaceNewKeywordSuggestion(context);
                }
            }
        }

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            var sourceString = text.ToString();

            //trigger completion automatically when assigning values
            var textBeforeCaret = sourceString.Substring(0, caretPosition);
            if (trigger.Kind == CompletionTriggerKind.Insertion
                && (textBeforeCaret.EndsWith(" = ") || textBeforeCaret.EndsWith("new ")))
            {
                return true;
            }
            return base.ShouldTriggerCompletion(text, caretPosition, trigger, options);
        }

        /// <summary>
        /// Return suggestions for special cases, such as array or collection initializers
        /// </summary>
        private IEnumerable<CompletionItem> GetSpecialCasesCompletions(ITypeSymbol typeSymbol, SyntaxContext syntaxContext)
        {
            if (typeSymbol is IArrayTypeSymbol)
            {
                return new[] { CompletionItemHelper.CreateCompletionItem("new [] {}", Sorting.WithPriority(3), newPositionOffset: -2) };
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
                            return new[] { CompletionItemHelper.CreateCompletionItem($"new List<{displayName}> {{}}", Sorting.WithPriority(1), newPositionOffset: -2) };
                        }
                        break;
                    case "Boolean":
                        return new[]
                        {
                             CompletionItemHelper.CreateCompletionItem("true", Sorting.WithPriority(3))
                                .WithTags(ImmutableArray.Create(CompletionTags.Keyword)),
                             CompletionItemHelper.CreateCompletionItem("false", Sorting.WithPriority(3))
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
