using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace IntelliSenseExtender.Extensions
{
    public static class ComplilationExtensions
    {
        /// <summary>
        /// If toSymbol is assignable from fromSymbol, and types are not generic - returns fromSymbol.
        /// If types are generic, and toSymbol is assignable from fromSymbol with certain type parameter  returns fromSymbol with that type parameter.
        /// If no conversion present - returns null
        /// </summary>
        public static ITypeSymbol GetAssignableSymbol(this Compilation compilation, ITypeSymbol fromSymbol, ITypeSymbol toSymbol)
        {
            if (compilation.ClassifyConversion(fromSymbol, toSymbol).IsImplicit)
            {
                return fromSymbol;
            }

            // Try construct symbol with type parameters for simple case - when arity is same, and parameters are corresponding.
            // TODO: think about complex cases (e.g. class SomeClass<T> : ISomeInterface<string, T>)
            if (fromSymbol is INamedTypeSymbol nFromSymbol
                && toSymbol is INamedTypeSymbol nToSymbol
                && nFromSymbol.Arity > 0 && nFromSymbol.Arity == nToSymbol.Arity)
            {
                var constructedFromSymbol = nFromSymbol.Construct(nToSymbol.TypeArguments.ToArray());
                if (compilation.ClassifyConversion(constructedFromSymbol, nToSymbol).IsImplicit)
                {
                    return constructedFromSymbol;
                }
            }
            return null;
        }
    }
}
