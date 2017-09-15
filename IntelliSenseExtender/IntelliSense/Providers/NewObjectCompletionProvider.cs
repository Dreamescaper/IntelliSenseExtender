using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IntelliSenseExtender.ExposedInternals;
using IntelliSenseExtender.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    [ExportCompletionProvider("Object creation provider", LanguageNames.CSharp)]
    public class NewObjectCompletionProvider : AbstractCompletionProvider
    {
        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var syntaxContext = await SyntaxContext.Create(context.Document, context.Position, context.CancellationToken);

            //ObjectCreationExpressionSyntax objCreation;
            //if (syntaxContext.SyntaxTree.IsObjectCreationContext(context.Position, out objCreation, context.CancellationToken))
            //{
            //    ITypeSymbol typeSymbol;
            //    if (TryGetTypeSymbol(objCreation, syntaxContext.SemanticModel, out typeSymbol))
            //    {
            //        var symbols = GetAllTypes(syntaxContext)
            //            .Select(type => syntaxContext.SemanticModel.Compilation.GetAssignableSymbol(type, typeSymbol))
            //            .Where(type => type != null)
            //            .ToList();

            //        var completionItems = symbols.Select(symbol =>
            //        {
            //            var symbolName = symbol.Name;
            //            var typeSymbolName = typeSymbol.Name;
            //            var priority = symbolName == typeSymbolName || "I" + symbolName == typeSymbolName
            //                ? Sorting.WithPriority(4)
            //                : Sorting.WithPriority(5);

            //            return CompletionItemHelper.CreateCompletionItem(symbol, syntaxContext, priority, MatchPriority.Preselect);
            //        })
            //            .ToList();

            //        context.AddItems(completionItems);
            //        context.AddItems(GetSpecialCasesCompletions(typeSymbol, syntaxContext));
            //    }
            //}

            SyntaxNode currentNode = syntaxContext.SyntaxTree.FindTokenOnLeftOfPosition(syntaxContext.Position, syntaxContext.CancellationToken).Parent;
            ITypeSymbol typeSymbol;
            if (TryGetTypeSymbol(syntaxContext, out typeSymbol))
            {
                var symbols = GetAllTypes(syntaxContext)
                    .Select(type => syntaxContext.SemanticModel.Compilation.GetAssignableSymbol(type, typeSymbol))
                    .Where(type => type != null)
                    .ToList();

                var completionItems = symbols.Select(symbol =>
                {
                    var symbolName = symbol.Name;
                    var typeSymbolName = typeSymbol.Name;
                    var priority = symbolName == typeSymbolName || "I" + symbolName == typeSymbolName
                        ? Sorting.WithPriority(4)
                        : Sorting.WithPriority(5);

                    bool unimported = !syntaxContext.ImportedNamespaces.Contains(symbol.GetNamespace());

                    return CompletionItemHelper.CreateCompletionItem(symbol, syntaxContext, priority, MatchPriority.Preselect, 0, unimported, true);
                })
                    .ToList();

                context.AddItems(completionItems);
                context.AddItems(GetSpecialCasesCompletions(typeSymbol, syntaxContext));
            }
        }

        private IEnumerable<CompletionItem> GetSpecialCasesCompletions(ITypeSymbol typeSymbol, SyntaxContext syntaxContext)
        {
            if (typeSymbol is IArrayTypeSymbol)
            {
                return new[] { CompletionItemHelper.CreateCompletionItem("new [] {};", Sorting.WithPriority(1), newPositionOffset: -2) };
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
                            return new[] { CompletionItemHelper.CreateCompletionItem($"new List<{displayName}> {{}};", Sorting.WithPriority(1), newPositionOffset: -2) };
                        }
                        break;
                }
            }

            return Enumerable.Empty<CompletionItem>();
        }

        protected override bool FilterType(INamedTypeSymbol type, SyntaxContext syntaxContext)
        {
            return
                (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct) &&
                !type.IsAbstract &&
                type.InstanceConstructors.Any(con => con.DeclaredAccessibility == Accessibility.Public);
        }

        private bool TryGetTypeSymbol(SyntaxContext syntaxContext, out ITypeSymbol typeSymbol)
        {
            SyntaxNode currentSyntaxNode = syntaxContext.CurrentToken.Parent;

            typeSymbol = null;

            if (currentSyntaxNode.Parent.Parent is VariableDeclarationSyntax varDeclarationSyntax
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
                typeSymbol = syntaxContext.SemanticModel.GetParameterTypeSymbol(argumentSyntax);
            }
            else if (currentSyntaxNode is ArgumentListSyntax argumentListSyntax)
            {
                int parameterIndex = argumentListSyntax.ChildTokens()
                    .Where(token => token.ValueText == ",")
                    .ToList().IndexOf(syntaxContext.CurrentToken) + 1;
                var parameters = syntaxContext.SemanticModel.GetParameters(argumentListSyntax);

                typeSymbol = parameters.ElementAtOrDefault(parameterIndex)?.Type;
            }

            return typeSymbol != null;
        }
    }
}
