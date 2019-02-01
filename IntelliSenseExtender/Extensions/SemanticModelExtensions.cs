using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using IntelliSenseExtender.IntelliSense.Context;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntelliSenseExtender.Extensions
{
    public static class SemanticModelExtensions
    {
        public static (ImmutableHashSet<INamespaceSymbol> importedNamespaces, ImmutableHashSet<ITypeSymbol> staticImports,
            ImmutableDictionary<INamespaceOrTypeSymbol, string> aliases)
            GetUsings(this SemanticModel semanticModel, SyntaxToken currentToken)
        {
            IEnumerable<INamespaceSymbol> GetParentNamespaces(INamespaceSymbol nsSymbol)
            {
                while (nsSymbol != null)
                {
                    yield return nsSymbol;
                    nsSymbol = nsSymbol.ContainingNamespace;
                }
            }

            if (currentToken.Parent == null)
            {
                return (ImmutableHashSet<INamespaceSymbol>.Empty,
                    ImmutableHashSet<ITypeSymbol>.Empty,
                    ImmutableDictionary<INamespaceOrTypeSymbol, string>.Empty);
            }

            var ancestors = currentToken.Parent.Ancestors().ToArray();

            var usings = ancestors
                .Select(a =>
                {
                    if (a is CompilationUnitSyntax compilationUnit)
                        return compilationUnit.Usings;
                    if (a is NamespaceDeclarationSyntax namespaceDeclaration)
                        return namespaceDeclaration.Usings;

                    return default;
                })
                .SelectMany(u => u)
                .ToArray();

            var importedNamespacesFromUsing = usings
                .Where(u => u.Alias == null && u.StaticKeyword.IsKind(SyntaxKind.None))
                .Select(ns => semanticModel.GetSymbolInfo(ns.Name).Symbol)
                .OfType<INamespaceSymbol>()
                .SelectMany(nsSymbol => nsSymbol.ConstituentNamespaces);

            var currentNamespaces = ancestors.OfType<NamespaceDeclarationSyntax>()
                .Select(ns => semanticModel.GetSymbolInfo(ns.Name).Symbol)
                .OfType<INamespaceSymbol>()
                .ToArray();

            if (currentNamespaces.Length == 0)
                currentNamespaces = new[] { semanticModel.Compilation.GlobalNamespace };

            var importedNamespaces = importedNamespacesFromUsing
                .Concat(currentNamespaces.SelectMany(GetParentNamespaces))
                .ToImmutableHashSet();

            var staticImports = usings
                .Where(u => u.Alias == null && u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
                .Select(ns => semanticModel.GetSymbolInfo(ns.Name).Symbol)
                .OfType<ITypeSymbol>()
                .ToImmutableHashSet();

            var aliases = usings
                .Where(u => u.Alias != null && u.StaticKeyword.IsKind(SyntaxKind.None))
                .Select(u => (
                    symbol: semanticModel.GetSymbolInfo(u.Name).Symbol as INamespaceOrTypeSymbol,
                    alias: u.Alias.Name.Identifier.Text))
                .Where(a => a.symbol != null)
                .GroupBy(a => a.symbol)
                .ToImmutableDictionary(g => g.Key, g => g.First().alias);

            return (importedNamespaces, staticImports, aliases);
        }

        public static InferredTypeInfo GetTypeSymbol(this SemanticModel semanticModel, SyntaxToken currentToken)
        {
            var inferredInfo = new InferredTypeInfo();


            SyntaxNode currentSyntaxNode = currentToken.Parent;

            // If new keyword is already present, we need to work with parent node
            if (currentSyntaxNode is ObjectCreationExpressionSyntax
                // happens with named arguments - we need to get ArgumentSyntax
                || currentSyntaxNode is NameColonSyntax)
            {
                currentSyntaxNode = currentSyntaxNode.Parent;
            }

            if (currentSyntaxNode?.Parent is VariableDeclaratorSyntax varDeclaratorSyntax
                && varDeclaratorSyntax.Parent is VariableDeclarationSyntax varDeclarationSyntax
                && !varDeclarationSyntax.Type.IsVar)
            {
                var typeInfo = semanticModel.GetTypeInfo(varDeclarationSyntax.Type);
                inferredInfo.Type = typeInfo.Type;
                inferredInfo.From = TypeInferredFrom.VariableDeclaration;
            }
            else if (currentSyntaxNode is AssignmentExpressionSyntax assigmentExpressionSyntax)
            {
                var typeInfo = semanticModel.GetTypeInfo(assigmentExpressionSyntax.Left);
                inferredInfo.Type = typeInfo.Type;
                inferredInfo.From = TypeInferredFrom.Assignment;
            }
            else if (currentSyntaxNode is ArgumentSyntax argumentSyntax)
            {
                var parameterSymbol = semanticModel.GetParameterSymbol(argumentSyntax);

                inferredInfo.Type = parameterSymbol?.Type;
                inferredInfo.From = TypeInferredFrom.MethodArgument;
                inferredInfo.ParameterSymbol = parameterSymbol;
            }
            else if (currentSyntaxNode is ArgumentListSyntax argumentListSyntax)
            {
                var parameterSymbol = semanticModel.GetParameterSymbol(argumentListSyntax, currentToken);

                inferredInfo.Type = parameterSymbol?.Type;
                inferredInfo.From = TypeInferredFrom.MethodArgument;
                inferredInfo.ParameterSymbol = parameterSymbol;
            }
            else if (currentSyntaxNode is ReturnStatementSyntax returnStatementSyntax)
            {
                inferredInfo.Type = semanticModel.GetReturnTypeSymbol(returnStatementSyntax);
                inferredInfo.From = TypeInferredFrom.ReturnValue;
            }
            else if (currentSyntaxNode is BinaryExpressionSyntax expressionSyntax)
            {
                inferredInfo.Type = semanticModel.GetTypeInfo(expressionSyntax.Left).Type;
                inferredInfo.From = TypeInferredFrom.BinaryExpression;
            }

            // If we have ValueTuple return value - try to infer element type
            else if (currentSyntaxNode is ParenthesizedExpressionSyntax
                && currentSyntaxNode.Parent is ReturnStatementSyntax parentReturnStatement)
            {
                // In such cases it's the first element - otherwise it would be TupleExpression

                var returnTypeSymbol = semanticModel.GetReturnTypeSymbol(parentReturnStatement);
                inferredInfo.Type = GetTupleTypeFromReturnValue(returnTypeSymbol, 0);
                inferredInfo.From = TypeInferredFrom.ReturnValue;
            }
            else if (currentSyntaxNode is TupleExpressionSyntax tupleSyntax
                && currentSyntaxNode.Parent is ReturnStatementSyntax parentReturn)
            {
                //In this case we have non-first element
                var tupleElementIndex = GetCurrentTupleElementIndex(tupleSyntax, currentToken);
                var returnTypeSymbol = semanticModel.GetReturnTypeSymbol(parentReturn);
                inferredInfo.Type = GetTupleTypeFromReturnValue(returnTypeSymbol, tupleElementIndex);
                inferredInfo.From = TypeInferredFrom.ReturnValue;
            }

            if (inferredInfo.Type == null)
            {
                inferredInfo.From = TypeInferredFrom.None;
            }

            return inferredInfo;
        }

        private static ITypeSymbol GetReturnTypeSymbol(this SemanticModel semanticModel, ReturnStatementSyntax returnStatementSyntax)
        {
            ITypeSymbol typeSymbol = null;

            var parentMethodOrProperty = returnStatementSyntax
                .Ancestors()
                .FirstOrDefault(node => node is MethodDeclarationSyntax || node is PropertyDeclarationSyntax);

            if (parentMethodOrProperty is MethodDeclarationSyntax methodSyntax)
            {
                var typeSyntax = methodSyntax.ReturnType;

                if (typeSyntax != null)
                    typeSymbol = semanticModel.GetTypeInfo(typeSyntax).Type;

                if (typeSymbol != null
                    && methodSyntax.Modifiers.Any(m => m.Kind() == SyntaxKind.AsyncKeyword)
                    && typeSymbol.Name == "Task"
                    && typeSymbol is INamedTypeSymbol namedTypeSymbol
                    && namedTypeSymbol.IsGenericType)
                {
                    typeSymbol = namedTypeSymbol.TypeArguments.FirstOrDefault();
                }
            }
            else if (parentMethodOrProperty is PropertyDeclarationSyntax propertySyntax)
            {
                var typeSyntax = propertySyntax.Type;
                if (typeSyntax != null)
                    typeSymbol = semanticModel.GetTypeInfo(typeSyntax).Type;
            }

            return typeSymbol;
        }

        private static IParameterSymbol GetParameterSymbol(this SemanticModel semanticModel, ArgumentSyntax argumentSyntax)
        {
            IParameterSymbol result = null;

            if (argumentSyntax.Parent is ArgumentListSyntax argumentListSyntax)
            {
                var parameters = GetParameters(semanticModel, argumentListSyntax);

                if (parameters != null)
                {
                    // If we have named argument - we don't care about order
                    var argumentName = argumentSyntax.NameColon?.Name.Identifier.Text;
                    if (argumentName != null)
                    {
                        result = parameters.FirstOrDefault(p => p.Name == argumentName);
                    }
                    // Otherwise - define parameter type by position
                    else
                    {
                        int paramIndex = argumentListSyntax.Arguments.IndexOf(argumentSyntax);
                        if (paramIndex != -1)
                        {
                            result = parameters.ElementAtOrDefault(paramIndex);
                        }
                    }
                }
            }

            return result;
        }

        private static IParameterSymbol GetParameterSymbol(this SemanticModel semanticModel, ArgumentListSyntax argumentListSyntax, SyntaxToken currentToken)
        {
            int parameterIndex = argumentListSyntax.ChildTokens()
                .Where(token => token.IsKind(SyntaxKind.CommaToken))
                .ToList().IndexOf(currentToken) + 1;
            var parameters = semanticModel.GetParameters(argumentListSyntax);

            return parameters?.ElementAtOrDefault(parameterIndex);
        }

        private static ITypeSymbol GetTupleTypeFromReturnValue(ITypeSymbol returnTypeSymbol, int elementIndex)
        {
            ITypeSymbol typeSymbol = null;

            if (returnTypeSymbol.IsTupleType
                && returnTypeSymbol is INamedTypeSymbol namedType
                && namedType.TupleElements.Length > elementIndex)
            {
                typeSymbol = namedType.TupleElements[elementIndex].Type;
            }

            return typeSymbol;
        }

        /// <summary>
        /// Return list of parameters of invoked method. Returns null if no method symbol found.
        /// </summary>
        private static IList<IParameterSymbol> GetParameters(this SemanticModel semanticModel, ArgumentListSyntax argumentListSyntax)
        {
            var invocationSyntax = argumentListSyntax.Parent;
            var methodInfo = semanticModel.GetSymbolInfo(invocationSyntax);

            IMethodSymbol methodSymbol = null;
            if (methodInfo.CandidateReason == CandidateReason.OverloadResolutionFailure
                && methodInfo.CandidateSymbols.Length > 1
                && argumentListSyntax.Arguments.Any(a => !a.IsMissing)
                && argumentListSyntax.Arguments.All(a => a.NameColon == null))
            {
                // If failed to resolve overload - try to find suitable based on existing parameters

                var presentArguments = argumentListSyntax.Arguments.TakeWhile(a => !a.IsMissing).ToList();

                methodSymbol = methodInfo.CandidateSymbols
                    .OfType<IMethodSymbol>()
                    .Where(s => s.Parameters.Length >= argumentListSyntax.Arguments.Count)
                    .FirstOrDefault(s =>
                    {
                        for (int i = 0; i < presentArguments.Count; i++)
                        {
                            if (!semanticModel.ClassifyConversion(presentArguments[i].Expression, s.Parameters[i].Type).IsImplicit)
                                return false;
                        }
                        return true;
                    });
            }
            else
            {
                methodSymbol = (methodInfo.Symbol ?? methodInfo.CandidateSymbols.FirstOrDefault()) as IMethodSymbol;
            }
            return methodSymbol?.Parameters;
        }

        private static int GetCurrentTupleElementIndex(TupleExpressionSyntax tupleSyntax, SyntaxToken currentToken)
        {
            // Simply count commas before current token
            var commasCount = tupleSyntax.ChildTokens()
                .Where(token => token.Kind() == SyntaxKind.CommaToken)
                .TakeWhile(token => token != currentToken)
                .Count();

            return commasCount;
        }
    }
}
