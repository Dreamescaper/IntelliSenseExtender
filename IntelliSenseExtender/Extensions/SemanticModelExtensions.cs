using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace IntelliSenseExtender.Extensions
{
    public static class SemanticModelExtensions
    {
        public static ITypeSymbol GetParameterTypeSymbol(this SemanticModel semanticModel, ArgumentSyntax argumentSyntax)
        {
            ITypeSymbol result = null;

            if (argumentSyntax.Parent is ArgumentListSyntax argumentListSyntax)
            {
                int paramIndex = argumentListSyntax.Arguments.IndexOf(argumentSyntax);
                if (paramIndex != -1
                   && argumentListSyntax.Parent is InvocationExpressionSyntax invocationSyntax)
                {
                    var methodInfo = semanticModel.GetSymbolInfo(invocationSyntax);
                    var methodSymbol = (methodInfo.Symbol ?? methodInfo.CandidateSymbols.FirstOrDefault()) as IMethodSymbol;

                    result = methodSymbol?.Parameters.ElementAtOrDefault(paramIndex)?.Type;
                }
            }

            return result;
        }
    }
}
