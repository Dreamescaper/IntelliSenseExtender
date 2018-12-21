using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public class LocalsCompletionProvider : ISimpleCompletionProvider, ITriggerCompletions
    {
        private static readonly Regex BracketRegex = new Regex(@"\w\($");
        private static readonly Regex AttributeArgumentRegex = new Regex(@"\[\w+\((|[^\]]+, )$");

        private static readonly string[] SymbolsToTriggerCompletion
            = new[] { ", ", "return ", "== ", "!= ", "> ", "< ", "<= ", ">= " };

        public IEnumerable<CompletionItem> GetCompletionItems(SyntaxContext syntaxContext, Options.Options options)
        {
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
            var thisCompletion = GetThisCompletionIfApplicable(syntaxContext);

            return localCompletions
                .Concat(lambdaParamsCompletions)
                .Concat(typeMemberCompletions)
                .Concat(methodParametersCompletions)
                .Concat(thisCompletion);
        }

        public bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, Options.Options options)
        {
            if (options.SuggestLocalVariablesFirst)
            {
                var currentLine = text.Lines.GetLineFromPosition(caretPosition);
                var textBeforeCaret = currentLine.ToString().Substring(0, caretPosition - currentLine.Start);

                if (trigger.Kind == CompletionTriggerKind.Insertion
                    && (BracketRegex.IsMatch(textBeforeCaret) || SymbolsToTriggerCompletion.Any(s => textBeforeCaret.EndsWith(s)))
                    && !AttributeArgumentRegex.IsMatch(textBeforeCaret))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsApplicable(SyntaxContext syntaxContext, Options.Options options)
        {
            return options.SuggestLocalVariablesFirst
                && syntaxContext.InferredType != null
                && (syntaxContext.TypeInferredFrom == TypeInferredFrom.MethodArgument
                    || syntaxContext.TypeInferredFrom == TypeInferredFrom.ReturnValue
                    || syntaxContext.TypeInferredFrom == TypeInferredFrom.Assignment
                    || syntaxContext.TypeInferredFrom == TypeInferredFrom.BinaryExpression);
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

                            // Simple local variables
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
                            // Deconstructed variables
                            else if (childNode.IsKind(SyntaxKind.ExpressionStatement))
                            {
                                var tupleDeclaration = childNode
                                    .ChildNodes().FirstOrDefault(n => n.IsKind(SyntaxKind.SimpleAssignmentExpression))
                                    ?.ChildNodes().FirstOrDefault(n => n.IsKind(SyntaxKind.DeclarationExpression))
                                    ?.ChildNodes().OfType<ParenthesizedVariableDesignationSyntax>().FirstOrDefault();

                                if (tupleDeclaration != null)
                                {
                                    foreach (var variable in tupleDeclaration.Variables)
                                    {
                                        yield return variable;
                                    }
                                }
                            }
                        }

                        currentNode = parentNode;
                        parentNode = currentNode.Parent;
                    }
                }
            }

            syntaxContext.CancellationToken.ThrowIfCancellationRequested();

            var symbols = getVariableSyntaxes()
                .Select(syntaxNode =>
                {
                    var declaredSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxNode);
                    Debug.Assert(declaredSymbol is ILocalSymbol, "Found Symbol is not ILocalSymbol!");
                    return declaredSymbol as ILocalSymbol;
                })
                .Where(symbol => symbol != null);

            return FilterUnneededSymbols(symbols, syntaxContext);
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

            var symbols = getLambdaParameterSyntaxes().Select(p => syntaxContext.SemanticModel.GetDeclaredSymbol(p));

            return FilterUnneededSymbols(symbols, syntaxContext);
        }

        private IEnumerable<ISymbol> GetTypeMembers(SyntaxContext syntaxContext)
        {
            syntaxContext.CancellationToken.ThrowIfCancellationRequested();

            var enclosingSymbol = syntaxContext.SemanticModel.GetEnclosingSymbol(syntaxContext.Position, syntaxContext.CancellationToken);

            var methodSymbol = enclosingSymbol?.AncestorsAndSelf().OfType<IMethodSymbol>().FirstOrDefault();
            var typeSymbol = enclosingSymbol?.ContainingType;

            if (typeSymbol == null || methodSymbol == null)
            {
                return Enumerable.Empty<ISymbol>();
            }

            var currentTypeMembers = typeSymbol.GetMembers()
                .Where(member => member.CanBeReferencedByName);
            var inheritedMembers = typeSymbol.GetBaseTypes()
                .SelectMany(type => type.GetMembers())
                .Where(member => member.CanBeReferencedByName
                    && (member.DeclaredAccessibility == Accessibility.Public
                    || member.DeclaredAccessibility == Accessibility.Protected));

            var fieldsAndProperties = currentTypeMembers.Union(inheritedMembers)
                .Where(member => member is IFieldSymbol || member is IPropertySymbol);

            //Don't suggest instance members in static method
            if (methodSymbol.IsStatic)
            {
                fieldsAndProperties = fieldsAndProperties.Where(member => member.IsStatic);
            }

            return FilterUnneededSymbols(fieldsAndProperties, syntaxContext);
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
            //Special case - property / indexed property set method
            else if (methodOrPropertyNode is AccessorDeclarationSyntax accessorNode
                && accessorNode.Kind() == SyntaxKind.SetAccessorDeclaration)
            {
                var basePropertyNode = accessorNode.Ancestors().OfType<BasePropertyDeclarationSyntax>().FirstOrDefault();

                if (basePropertyNode is PropertyDeclarationSyntax propertyNode)
                {
                    var propertySymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(propertyNode);
                    return propertySymbol.SetMethod.Parameters;
                }
                else if (basePropertyNode is IndexerDeclarationSyntax indexerNode)
                {
                    var propertySymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(indexerNode);
                    return propertySymbol.SetMethod.Parameters;
                }
            }

            return Enumerable.Empty<IParameterSymbol>();
        }

        private IEnumerable<T> GetAssignableSymbols<T>(SyntaxContext syntaxContext, IEnumerable<T> symbols,
            Func<T, ITypeSymbol> getSymbolType, ITypeSymbol toSymbol) where T : ISymbol
        {
            return symbols.Where(symbol =>
                syntaxContext.SemanticModel.Compilation.ClassifyConversion(getSymbolType(symbol), toSymbol).IsImplicit);
        }

        private IEnumerable<TSymbol> FilterUnneededSymbols<TSymbol>(IEnumerable<TSymbol> symbols, SyntaxContext syntaxContext)
            where TSymbol : ISymbol
        {
            var currentSyntax = syntaxContext.CurrentToken.Parent;

            // Do not suggest self for assignments
            if (currentSyntax is AssignmentExpressionSyntax assignment)
            {
                var assignedSymbol = syntaxContext.SemanticModel
                    .GetSymbolInfo(assignment.Left, syntaxContext.CancellationToken)
                    .Symbol;

                if (assignedSymbol != null)
                {
                    symbols = symbols.Where(s => !s.Equals(assignedSymbol));
                }
            }
            // Do not suggest self for binary expressions
            else if (currentSyntax is BinaryExpressionSyntax binaryExpression)
            {
                var assignedSymbol = syntaxContext.SemanticModel
                    .GetSymbolInfo(binaryExpression.Left, syntaxContext.CancellationToken)
                    .Symbol;

                if (assignedSymbol != null)
                {
                    symbols = symbols.Where(s => !s.Equals(assignedSymbol));
                }
            }

            return symbols;
        }

        private IEnumerable<CompletionItem> GetThisCompletionIfApplicable(SyntaxContext syntaxContext)
        {
            var enclosingSymbol = syntaxContext.SemanticModel.GetEnclosingSymbol(syntaxContext.Position, syntaxContext.CancellationToken);

            var methodSymbol = enclosingSymbol?.AncestorsAndSelf().OfType<IMethodSymbol>().FirstOrDefault();
            var typeSymbol = enclosingSymbol?.ContainingType;

            if (typeSymbol == null || methodSymbol == null || methodSymbol.IsStatic)
                return Enumerable.Empty<CompletionItem>();

            var typeMatches = syntaxContext.SemanticModel.Compilation
                .ClassifyConversion(typeSymbol, syntaxContext.InferredType).IsImplicit;
            if (!typeMatches)
                return Enumerable.Empty<CompletionItem>();

            var newSuggestion = CompletionItemHelper.CreateCompletionItem(
                "this",
                Sorting.Suitable_Locals,
                ImmutableArray.Create(WellKnownTags.Keyword));

            return new[] { newSuggestion };
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
