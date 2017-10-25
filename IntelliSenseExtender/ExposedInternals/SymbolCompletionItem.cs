using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.ExposedInternals
{
    /// <summary>
    /// Exposing some methods from internal class
    /// <see cref="Microsoft.CodeAnalysis.Completion.Providers.SymbolCompletionItem"/>
    /// </summary>
    public static class SymbolCompletionItem
    {
        private static readonly Type _internalType;
        private static readonly MethodInfo _encodeSymbolMethod;
        private static readonly MethodInfo _getDescriptionAsyncMethod;

        static SymbolCompletionItem()
        {
            _internalType = Type.GetType("Microsoft.CodeAnalysis.Completion.Providers.SymbolCompletionItem," +
                "Microsoft.CodeAnalysis.Features");
            _encodeSymbolMethod = _internalType.GetMethod("EncodeSymbol");
            _getDescriptionAsyncMethod = _internalType.GetMethods()
                .Single(method => method.Name == "GetDescriptionAsync"
                    && method.GetParameters().Length == 3);
        }

        public static Task<CompletionDescription> GetDescriptionAsync
            (CompletionItem item, Document document, CancellationToken cancellationToken)
        {
            return (Task<CompletionDescription>)_getDescriptionAsyncMethod.Invoke(null,
                new object[] { item, document, cancellationToken });
        }

        public static string EncodeSymbol(ISymbol symbol)
        {
            return (string)_encodeSymbolMethod.Invoke(null, new object[] { symbol });
        }
    }
}