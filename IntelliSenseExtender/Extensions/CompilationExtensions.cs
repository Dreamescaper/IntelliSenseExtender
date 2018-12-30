using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IntelliSenseExtender.Extensions
{
    public static class CompilationExtensions
    {
        /// <summary>
        /// If toSymbol is assignable from fromSymbol, and types are not generic - returns fromSymbol.
        /// If types are generic, and toSymbol is assignable from fromSymbol with certain type parameter  returns fromSymbol with that type parameter.
        /// If no conversion present - returns null
        /// </summary>
        public static ITypeSymbol GetAssignableSymbol(this Compilation compilation, ITypeSymbol fromSymbol, ITypeSymbol toSymbol)
        {
            if (IsAssignable(compilation, fromSymbol, toSymbol))
            {
                return fromSymbol;
            }

            // Try construct symbol with type parameters for simple case - when arity is same, and parameters are corresponding.
            // TODO: think about complex cases (e.g. class SomeClass<T> : ISomeInterface<string, T>)
            if (fromSymbol is INamedTypeSymbol nFromSymbol
                && toSymbol is INamedTypeSymbol nToSymbol
                && nFromSymbol.Arity > 0
                && nFromSymbol.Arity == nToSymbol.Arity)
            {
                var toTypeAgruments = nToSymbol.TypeArguments.ToArray();
                var constructedFromSymbol = nFromSymbol.Construct(toTypeAgruments);
                if (IsAssignable(compilation, constructedFromSymbol, nToSymbol))
                {
                    // Verify if type parameters constraints (e.g. 'where T:IComparable') are satisfied
                    var typeParametersSatisfyConditions = nFromSymbol.TypeParameters
                            .Select((typeParam, i) => typeParam.ConstraintTypes
                                .All(constraintType => IsAssignable(compilation, toTypeAgruments[i], constraintType)))
                            .All(satisfies => satisfies);
                    if (typeParametersSatisfyConditions)
                    {
                        return constructedFromSymbol;
                    }
                }
            }
            return null;
        }

        private static bool IsAssignable(Compilation compilation, ITypeSymbol fromSymbol, ITypeSymbol toSymbol)
        {
            var conversion = compilation.ClassifyConversion(fromSymbol, toSymbol);
            return conversion.IsImplicit && !conversion.IsUserDefined;
        }
    }
}
