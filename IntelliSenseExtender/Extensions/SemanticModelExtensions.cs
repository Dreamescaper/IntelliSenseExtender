using System.Collections.Generic;
using System.Linq;
using IntelliSenseExtender.IntelliSense.Context;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntelliSenseExtender.Extensions
{
    public static class SemanticModelExtensions
    {
        public static (ITypeSymbol typeSymbol, TypeInferredFrom inferredFrom) GetTypeSymbol(this SemanticModel semanticModel, SyntaxToken currentToken)
        {
            ITypeSymbol typeSymbol = null;
            TypeInferredFrom inferredFrom = TypeInferredFrom.None;

            SyntaxNode currentSyntaxNode = currentToken.Parent;

            // If new keyword is already present, we need to work with parent node
            if (currentSyntaxNode is ObjectCreationExpressionSyntax
                // happens with named arguments - we need to get ArgumentSyntax
                || currentSyntaxNode is NameColonSyntax)
            {
                currentSyntaxNode = currentSyntaxNode.Parent;
            }

            if (currentSyntaxNode?.Parent?.Parent is VariableDeclarationSyntax varDeclarationSyntax
                && !varDeclarationSyntax.Type.IsVar)
            {
                var typeInfo = semanticModel.GetTypeInfo(varDeclarationSyntax.Type);
                typeSymbol = typeInfo.Type;
                inferredFrom = TypeInferredFrom.VariableDeclaration;
            }
            else if (currentSyntaxNode is AssignmentExpressionSyntax assigmentExpressionSyntax)
            {
                var typeInfo = semanticModel.GetTypeInfo(assigmentExpressionSyntax.Left);
                typeSymbol = typeInfo.Type;
                inferredFrom = TypeInferredFrom.Assignment;
            }
            else if (currentSyntaxNode is ArgumentSyntax argumentSyntax)
            {
                typeSymbol = semanticModel.GetArgumentTypeSymbol(argumentSyntax);
                inferredFrom = TypeInferredFrom.MethodArgument;
            }
            else if (currentSyntaxNode is ArgumentListSyntax argumentListSyntax)
            {
                typeSymbol = semanticModel.GetArgumentTypeSymbol(argumentListSyntax, currentToken);
                inferredFrom = TypeInferredFrom.MethodArgument;
            }
            else if (currentSyntaxNode is ReturnStatementSyntax returnStatementSyntax)
            {
                typeSymbol = semanticModel.GetReturnTypeSymbol(returnStatementSyntax);
                inferredFrom = TypeInferredFrom.ReturnValue;
            }

            // If we have ValueTuple return value - try to infer element type
            else if (currentSyntaxNode is ParenthesizedExpressionSyntax
                && currentSyntaxNode.Parent is ReturnStatementSyntax parentReturnStatement)
            {
                // In such cases it's the first element - otherwise it would be TupleExpression

                var returnTypeSymbol = semanticModel.GetReturnTypeSymbol(parentReturnStatement);
                typeSymbol = GetTupleTypeFromReturnValue(returnTypeSymbol, 0);
                inferredFrom = TypeInferredFrom.ReturnValue;
            }
            else if (currentSyntaxNode is TupleExpressionSyntax tupleSyntax
                && currentSyntaxNode.Parent is ReturnStatementSyntax parentReturn)
            {
                //In this case we have non-first element
                var tupleElementIndex = GetCurrentTupleElementIndex(tupleSyntax, currentToken);
                var returnTypeSymbol = semanticModel.GetReturnTypeSymbol(parentReturn);
                typeSymbol = GetTupleTypeFromReturnValue(returnTypeSymbol, tupleElementIndex);
                inferredFrom = TypeInferredFrom.ReturnValue;
            }

            if (typeSymbol == null)
            {
                inferredFrom = TypeInferredFrom.None;
            }

            return (typeSymbol, inferredFrom);
        }

        private static ITypeSymbol GetReturnTypeSymbol(this SemanticModel semanticModel, ReturnStatementSyntax returnStatementSyntax)
        {
            ITypeSymbol typeSymbol = null;

            var parentMethodOrProperty = returnStatementSyntax
                .Ancestors()
                .FirstOrDefault(node => node is MethodDeclarationSyntax || node is PropertyDeclarationSyntax);

            var typeSyntax = (parentMethodOrProperty as MethodDeclarationSyntax)?.ReturnType
                ?? (parentMethodOrProperty as PropertyDeclarationSyntax)?.Type;
            if (typeSyntax != null)
            {
                typeSymbol = semanticModel.GetTypeInfo(typeSyntax).Type;
            }

            return typeSymbol;
        }

        private static ITypeSymbol GetArgumentTypeSymbol(this SemanticModel semanticModel, ArgumentSyntax argumentSyntax)
        {
            ITypeSymbol result = null;

            if (argumentSyntax.Parent is ArgumentListSyntax argumentListSyntax)
            {
                var parameters = GetParameters(semanticModel, argumentListSyntax);

                if (parameters != null)
                {
                    // If we have named argument - we don't care about order
                    var argumentName = argumentSyntax.NameColon?.Name.Identifier.Text;
                    if (argumentName != null)
                    {
                        result = parameters.FirstOrDefault(p => p.Name == argumentName)?.Type;
                    }
                    // Otherwise - define parameter type by position
                    else
                    {
                        int paramIndex = argumentListSyntax.Arguments.IndexOf(argumentSyntax);
                        if (paramIndex != -1)
                        {
                            result = parameters.ElementAtOrDefault(paramIndex)?.Type;
                        }
                    }
                }
            }

            return result;
        }

        private static ITypeSymbol GetArgumentTypeSymbol(this SemanticModel semanticModel, ArgumentListSyntax argumentListSyntax, SyntaxToken currentToken)
        {
            int parameterIndex = argumentListSyntax.ChildTokens()
                .Where(token => token.IsKind(SyntaxKind.CommaToken))
                .ToList().IndexOf(currentToken) + 1;
            var parameters = semanticModel.GetParameters(argumentListSyntax);

            return parameters?.ElementAtOrDefault(parameterIndex)?.Type;
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
            var methodInfo = semanticModel.GetSymbolInfo(node: invocationSyntax);
            var methodSymbol = (methodInfo.Symbol ?? methodInfo.CandidateSymbols.FirstOrDefault()) as IMethodSymbol;

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
