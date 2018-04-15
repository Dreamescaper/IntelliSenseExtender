using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using IntelliSenseExtender.Context;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.IntelliSense.Context;
using IntelliSenseExtender.IntelliSense.Providers.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public class LocalsCompletionProvider : ISimpleCompletionProvider, ITriggerCompletions
    {
        private static readonly Regex BracketRegex = new Regex(@"\w\($");

        public IEnumerable<CompletionItem> GetCompletionItems(SyntaxContext syntaxContext, Options.Options options)
        {
            if (!options.SuggestLocalVariablesFirst
                || (syntaxContext.TypeInferredFrom != TypeInferredFrom.MethodArgument
                && syntaxContext.TypeInferredFrom != TypeInferredFrom.ReturnValue))
            {
                return null;
            }

            var typeSymbol = syntaxContext.InferredType;
            var locals = GetLocalVariables(syntaxContext);
            var suitableLocals = GetAssignableSymbols(syntaxContext, locals, s => s.Type, typeSymbol);

            var lambdaParameters = GetLambdaParameters(syntaxContext);
            var suitableLambdaParameters = GetAssignableSymbols(syntaxContext, lambdaParameters, s => s.Type, typeSymbol);

            var typeMembers = GetTypeMembers(syntaxContext);
            ITypeSymbol getMemberType(ISymbol s) => (s as IFieldSymbol)?.Type ?? ((IPropertySymbol)s).Type;
            var suitableTypeMembers = GetAssignableSymbols(syntaxContext, typeMembers, getMemberType, typeSymbol);

            var methodParameters = GetMethodParameters(syntaxContext);
            var suitableMethodParameters = GetAssignableSymbols(syntaxContext, methodParameters, s => s.Type, typeSymbol);

            var localCompletions = suitableLocals
                .Select(symbol => CreateCompletion(syntaxContext, symbol, Sorting.Suitable_Locals));
            var lambdaParamsCompletions = suitableLambdaParameters
                .Select(symbol => CreateCompletion(syntaxContext, symbol, Sorting.Suitable_LambdaParameters));
            var typeMemberCompletions = suitableTypeMembers
                .Select(symbol => CreateCompletion(syntaxContext, symbol, Sorting.Suitable_TypeMembers));
            var methodParametersCompletions = suitableMethodParameters
                .Select(l => CreateCompletion(syntaxContext, l, Sorting.Suitable_MethodParameters));

            return localCompletions
                .Concat(lambdaParamsCompletions)
                .Concat(typeMemberCompletions)
                .Concat(methodParametersCompletions);
        }

        public bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, Options.Options options)
        {
            if (options.SuggestLocalVariablesFirst)
            {
                var sourceString = text.ToString();

                var textBeforeCaret = sourceString.Substring(0, caretPosition);
                if (trigger.Kind == CompletionTriggerKind.Insertion
                    && (BracketRegex.IsMatch(textBeforeCaret) || textBeforeCaret.EndsWith(", ") || textBeforeCaret.EndsWith("return ")))
                {
                    return true;
                }
            }
            return false;
        }

        private IEnumerable<ILocalSymbol> GetLocalVariables(SyntaxContext syntaxContext)
        {
            IEnumerable<SyntaxNode> getVariableSyntaxes()
            {
                var currentNode = syntaxContext.CurrentToken.Parent;
                var parentNode = currentNode?.Parent;
                var containingMethodNode = parentNode?.AncestorsAndSelf()
                    .FirstOrDefault(node =>
                        node is MethodDeclarationSyntax
                        || node is AccessorDeclarationSyntax
                        || node is ConstructorDeclarationSyntax);

                if (containingMethodNode != null)
                {
                    while (parentNode != null && currentNode != containingMethodNode)
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
            }

            syntaxContext.CancellationToken.ThrowIfCancellationRequested();

            return getVariableSyntaxes()
                .Select(syntaxNode =>
                {
                    var declaredSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxNode);
                    Debug.Assert(declaredSymbol is ILocalSymbol, "Found Symbol is not ILocalSymbol!");
                    return declaredSymbol as ILocalSymbol;
                })
                .Where(symbol => symbol != null);
        }

        private IEnumerable<IParameterSymbol> GetLambdaParameters(SyntaxContext syntaxContext)
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

            var methodSymbol = enclosingSymbol.AncestorsAndSelf().OfType<IMethodSymbol>().FirstOrDefault();
            var typeSymbol = enclosingSymbol.ContainingType;

            if (typeSymbol == null || methodSymbol == null)
            {
                return Enumerable.Empty<ISymbol>();
            }

            var currentTypeMembers = typeSymbol.GetMembers();
            var inheritedMembers = typeSymbol.GetBaseTypes()
                .SelectMany(type => type.GetMembers())
                .Where(member => member.DeclaredAccessibility == Accessibility.Public
                    || member.DeclaredAccessibility == Accessibility.Protected);

            var fieldsAndProperties = currentTypeMembers.Union(inheritedMembers)
                .Where(member => member is IFieldSymbol || member is IPropertySymbol);

            //Don't suggest instance members in static method
            if (methodSymbol.IsStatic)
            {
                fieldsAndProperties = fieldsAndProperties.Where(member => member.IsStatic);
            }

            return fieldsAndProperties;
        }

        private IEnumerable<IParameterSymbol> GetMethodParameters(SyntaxContext syntaxContext)
        {
            syntaxContext.CancellationToken.ThrowIfCancellationRequested();

            var currentNode = syntaxContext.CurrentToken.Parent;

            var methodOrPropertyNode = currentNode.AncestorsAndSelf()
                .FirstOrDefault(node => node is MethodDeclarationSyntax || node is AccessorDeclarationSyntax);

            if (methodOrPropertyNode is MethodDeclarationSyntax methodNode)
            {
                var methodSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(methodNode);
                return methodSymbol.Parameters;
            }
            //Special case - property set method
            else if (methodOrPropertyNode is AccessorDeclarationSyntax accessorNode
                && accessorNode.Kind() == SyntaxKind.SetAccessorDeclaration)
            {
                var propertyNode = accessorNode.Ancestors().OfType<PropertyDeclarationSyntax>().First();
                var propertySymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(propertyNode);
                return propertySymbol.SetMethod.Parameters;
            }

            return Enumerable.Empty<IParameterSymbol>();
        }

        private IEnumerable<T> GetAssignableSymbols<T>(SyntaxContext syntaxContext, IEnumerable<T> symbols,
            Func<T, ITypeSymbol> getSymbolType, ITypeSymbol toSymbol) where T : ISymbol
        {
            return symbols.Where(symbol =>
                syntaxContext.SemanticModel.Compilation.ClassifyConversion(getSymbolType(symbol), toSymbol).IsImplicit);
        }

        private CompletionItem CreateCompletion(SyntaxContext syntaxContext, ISymbol symbol, int sorting)
        {
            return CompletionItemHelper.CreateCompletionItem(symbol, syntaxContext,
                unimported: false,
                matchPriority: MatchPriority.Preselect,
                sortingPriority: sorting);
        }
    }
}
