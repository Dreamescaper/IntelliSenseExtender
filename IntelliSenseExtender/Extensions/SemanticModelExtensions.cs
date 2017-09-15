using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
                if (paramIndex != -1)
                {
                    return GetParameters(semanticModel, argumentListSyntax).ElementAtOrDefault(paramIndex)?.Type;
                }
            }

            return result;
        }

        public static IList<IParameterSymbol> GetParameters(this SemanticModel semanticModel, ArgumentListSyntax argumentListSyntax)
        {
            var invocationSyntax = (InvocationExpressionSyntax)argumentListSyntax.Parent;
            var methodInfo = semanticModel.GetSymbolInfo(invocationSyntax);
            var methodSymbol = (methodInfo.Symbol ?? methodInfo.CandidateSymbols.FirstOrDefault()) as IMethodSymbol;

            return methodSymbol.Parameters;
        }
    }
}
