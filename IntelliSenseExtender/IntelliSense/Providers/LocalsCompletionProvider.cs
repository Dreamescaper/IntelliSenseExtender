using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    [ExportCompletionProvider(nameof(LocalsCompletionProvider), LanguageNames.CSharp)]
    public class LocalsCompletionProvider : AbstractCompletionProvider
    {
        public LocalsCompletionProvider() : base()
        {
        }

        public LocalsCompletionProvider(IOptionsProvider optionsProvider) : base(optionsProvider)
        {
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            if (Options.SuggestLocalVariablesFirst)
            {
                var syntaxContext = await SyntaxContext.Create(context.Document, context.Position, context.CancellationToken).ConfigureAwait(false);
                var locals = GetLocalVariables(syntaxContext)
                    .Union(GetLambdaParameters(syntaxContext))
                    .Union(GetTypeMembers(syntaxContext))
                    .Union(GetMethodParameters(syntaxContext))
                    .Select(localSymbol =>
                    CompletionItemHelper.CreateCompletionItem(localSymbol, syntaxContext, unimported: false));

                context.AddItems(locals);
            }
        }

        private IEnumerable<ILocalSymbol> GetLocalVariables(SyntaxContext syntaxContext)
        {
            IEnumerable<SyntaxNode> getVariableSyntaxes()
            {
                var currentNode = syntaxContext.CurrentToken.Parent;
                var parentNode = currentNode?.Parent;

                while (parentNode != null
                    && !(currentNode is MethodDeclarationSyntax)
                    && !(currentNode is PropertyDeclarationSyntax))
                {
                    // foreach and using statements
                    if (parentNode is ForEachStatementSyntax
                        || parentNode is VariableDeclaratorSyntax)
                    {
                        yield return parentNode;
                    }

                    // for statement
                    else if (parentNode is ForStatementSyntax)
                    {
                        var varDeclaratorSyntax = parentNode
                            .ChildNodes().FirstOrDefault(node => node is VariableDeclarationSyntax)
                            ?.ChildNodes().FirstOrDefault(node => node is VariableDeclaratorSyntax);
                        if (varDeclaratorSyntax != null)
                        {
                            yield return varDeclaratorSyntax;
                        }
                    }

                    // is pattern variable
                    // Currently only isPattern inside parent 'if' is supported
                    else if (parentNode is IfStatementSyntax)
                    {
                        var patternDeclarator = parentNode
                            .ChildNodes().FirstOrDefault(node => node is IsPatternExpressionSyntax)
                            ?.ChildNodes().FirstOrDefault(node => node is DeclarationPatternSyntax)
                            ?.ChildNodes().FirstOrDefault(node => node is SingleVariableDesignationSyntax);

                        if (patternDeclarator != null)
                        {
                            yield return patternDeclarator;
                        }
                    }

                    foreach (var childNode in parentNode.ChildNodes())
                    {
                        // We don't need to look further then current node
                        if (childNode == currentNode)
                        {
                            break;
                        }

                        if (childNode is LocalDeclarationStatementSyntax)
                        {
                            var varDeclaratorSyntax = childNode
                                .ChildNodes().FirstOrDefault(node => node is VariableDeclarationSyntax)
                                ?.ChildNodes().FirstOrDefault(node => node is VariableDeclaratorSyntax);
                            if (varDeclaratorSyntax != null)
                            {
                                yield return varDeclaratorSyntax;
                            }
                        }
                    }

                    currentNode = parentNode;
                    parentNode = currentNode.Parent;
                }
            }

            syntaxContext.CancellationToken.ThrowIfCancellationRequested();

            return getVariableSyntaxes().Select(syntaxNode =>
            {
                var declaredSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxNode);
                Debug.Assert(declaredSymbol is ILocalSymbol, "Found Symbol is not ILocalSymbol!");
                return declaredSymbol as ILocalSymbol;
            });
        }

        private IEnumerable<ISymbol> GetLambdaParameters(SyntaxContext syntaxContext)
        {
            IEnumerable<ParameterSyntax> getLambdaParameterSyntaxes()
            {
                var currentNode = syntaxContext.CurrentToken.Parent;
                var lambdas = currentNode?.Ancestors().Where(node => node is LambdaExpressionSyntax);

                if (lambdas != null)
                {
                    foreach (var lambdaSyntax in lambdas)
                    {
                        if (lambdaSyntax is SimpleLambdaExpressionSyntax simpleLambda)
                        {
                            yield return simpleLambda.Parameter;
                        }
                        else if (lambdaSyntax is ParenthesizedLambdaExpressionSyntax parenthesizedLambda)
                        {
                            foreach (var parameter in parenthesizedLambda.ParameterList.Parameters)
                            {
                                yield return parameter;
                            }
                        }
                    }
                }
            }

            syntaxContext.CancellationToken.ThrowIfCancellationRequested();

            return getLambdaParameterSyntaxes().Select(p => syntaxContext.SemanticModel.GetDeclaredSymbol(p));
        }

        private IEnumerable<ISymbol> GetTypeMembers(SyntaxContext syntaxContext)
        {
            syntaxContext.CancellationToken.ThrowIfCancellationRequested();

            var enclosingSymbol = syntaxContext.SemanticModel.GetEnclosingSymbol(syntaxContext.Position, syntaxContext.CancellationToken);
            var typeSymbol = enclosingSymbol.ContainingType;

            if (typeSymbol == null)
            {
                return Enumerable.Empty<ISymbol>();
            }

            var currentTypeMembers = typeSymbol.GetMembers();
            var inheritedMembers = typeSymbol.GetBaseTypes()
                .SelectMany(type => type.GetMembers())
                .Where(member => member.DeclaredAccessibility == Accessibility.Public
                    || member.DeclaredAccessibility == Accessibility.Protected);

            return currentTypeMembers.Union(inheritedMembers)
                .Where(member => member is IFieldSymbol || member is IPropertySymbol);
        }

        private IEnumerable<ISymbol> GetMethodParameters(SyntaxContext syntaxContext)
        {
            syntaxContext.CancellationToken.ThrowIfCancellationRequested();

            var currentNode = syntaxContext.CurrentToken.Parent;
            var methodNode = currentNode.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();

            if (methodNode == null)
            {
                return Enumerable.Empty<ISymbol>();
            }

            var methodSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(methodNode);
            return methodSymbol.Parameters;
        }
    }
}
