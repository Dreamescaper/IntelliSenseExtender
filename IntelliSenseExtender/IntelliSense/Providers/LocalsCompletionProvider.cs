using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IntelliSenseExtender.ExposedInternals;
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
    public class LocalsCompletionProvider : ICompletionProvider, ITriggerCompletions
    {
        private static readonly Regex BracketRegex = new Regex(@"\w\($");

        private static readonly string[] SymbolsToTriggerCompletion
            = new[] { ", ", "return ", "== ", "!= ", "> ", "< ", "<= ", ">= ", ": " };

        public Task<IEnumerable<CompletionItem>> GetCompletionItemsAsync(SyntaxContext syntaxContext, Options.Options options)
        {
            var lookedUpSymbols = syntaxContext.SemanticModel.LookupSymbols(syntaxContext.Position);

            var typeSymbol = syntaxContext.InferredInfo.Type!;
            var locals = GetLocalVariables(lookedUpSymbols, syntaxContext);
            var suitableLocals = GetAssignableSymbols(syntaxContext, locals, s => s.Type, typeSymbol);

            var lambdaParameters = GetLambdaParameters(lookedUpSymbols, syntaxContext);
            var suitableLambdaParameters = GetAssignableSymbols(syntaxContext, lambdaParameters, s => s.Type, typeSymbol);

            var typeMembers = GetTypeMembers(lookedUpSymbols, syntaxContext);
            ITypeSymbol getMemberType(ISymbol s) => (s as IFieldSymbol)?.Type ?? ((IPropertySymbol)s).Type;
            var suitableTypeMembers = GetAssignableSymbols(syntaxContext, typeMembers, getMemberType, typeSymbol);

            var methodParameters = GetMethodParameters(lookedUpSymbols, syntaxContext);
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

            return Task.FromResult(localCompletions
                .Concat(lambdaParamsCompletions)
                .Concat(typeMemberCompletions)
                .Concat(methodParametersCompletions)
                .Concat(thisCompletion));
        }

        public bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, Options.Options options)
        {
            if (options.SuggestLocalVariablesFirst)
            {
                var currentLine = text.Lines.GetLineFromPosition(caretPosition);
                var textBeforeCaret = currentLine.ToString().Substring(0, caretPosition - currentLine.Start);

                if (trigger.Kind == CompletionTriggerKind.Insertion
                    && (BracketRegex.IsMatch(textBeforeCaret) || SymbolsToTriggerCompletion.Any(s => textBeforeCaret.EndsWith(s))))
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsApplicable(SyntaxContext syntaxContext, Options.Options options)
        {
            return options.SuggestLocalVariablesFirst
                && syntaxContext.InferredInfo.Type != null
                && (syntaxContext.InferredInfo.From == TypeInferredFrom.MethodArgument
                    || syntaxContext.InferredInfo.From == TypeInferredFrom.ReturnValue
                    || syntaxContext.InferredInfo.From == TypeInferredFrom.Assignment
                    || syntaxContext.InferredInfo.From == TypeInferredFrom.BinaryExpression);
        }

        private IEnumerable<ILocalSymbol> GetLocalVariables(IEnumerable<ISymbol> availableSymbols, SyntaxContext syntaxContext)
        {
            var symbols = availableSymbols
                .OfType<ILocalSymbol>()
                .Where(s => !s.IsInaccessibleLocal(syntaxContext.Position));

            return FilterUnneededSymbols(symbols, syntaxContext);
        }

        private IEnumerable<IParameterSymbol> GetLambdaParameters(IEnumerable<ISymbol> availableSymbols, SyntaxContext syntaxContext)
        {
            var symbols = availableSymbols
                .OfType<IParameterSymbol>()
                .Where(ps => ps.ContainingSymbol is IMethodSymbol ms
                    && ms.MethodKind == MethodKind.LambdaMethod);

            return FilterUnneededSymbols(symbols, syntaxContext);
        }

        private IEnumerable<ISymbol> GetTypeMembers(IEnumerable<ISymbol> availableSymbols, SyntaxContext syntaxContext)
        {
            syntaxContext.CancellationToken.ThrowIfCancellationRequested();

            // Strange that SemanticModel.LookupSymbols returns instance members in static context. Bug?
            var enclosingMethodSymbol = syntaxContext.SemanticModel
                .GetEnclosingSymbol(syntaxContext.Position, syntaxContext.CancellationToken)
                ?.AncestorsAndSelf().OfType<IMethodSymbol>()
                .FirstOrDefault();

            var fieldsAndProperties = availableSymbols
                .Where(s => (s.Kind == SymbolKind.Field || s.Kind == SymbolKind.Property)
                    && (enclosingMethodSymbol?.IsStatic != true || s.IsStatic));

            return FilterUnneededSymbols(fieldsAndProperties, syntaxContext);
        }

        private IEnumerable<IParameterSymbol> GetMethodParameters(IEnumerable<ISymbol> availableSymbols, SyntaxContext syntaxContext)
        {
            syntaxContext.CancellationToken.ThrowIfCancellationRequested();

            var symbols = availableSymbols
                .OfType<IParameterSymbol>()
                .Where(ps => ps.ContainingSymbol is IMethodSymbol ms
                    && ms.MethodKind != MethodKind.LambdaMethod);

            return FilterUnneededSymbols(symbols, syntaxContext);
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

            if (typeSymbol == null || methodSymbol == null || methodSymbol.IsStatic || syntaxContext.InferredInfo.Type is null)
                return Enumerable.Empty<CompletionItem>();

            var typeMatches = syntaxContext.SemanticModel.Compilation
                .ClassifyConversion(typeSymbol, syntaxContext.InferredInfo.Type).IsImplicit;
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
