using System;
using System.Linq;
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
        private static readonly EncodeSymbolHandler _encodeSymbolMethod;
        private static readonly GetDescriptionAsyncHandler _getDescriptionAsyncMethod;

        private delegate Task<CompletionDescription> GetDescriptionAsyncHandler(CompletionItem item, Document document, CancellationToken cancellationToken);

        private delegate string EncodeSymbolHandler(ISymbol symbol);

        static SymbolCompletionItem()
        {
            var featuresAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .First(a => a.GetName().Name == "Microsoft.CodeAnalysis.Features");
            var _internalType = featuresAssembly.GetType("Microsoft.CodeAnalysis.Completion.Providers.SymbolCompletionItem");

            _encodeSymbolMethod = (EncodeSymbolHandler)_internalType
                .GetMethod("EncodeSymbol", new[] { typeof(ISymbol) })
                .CreateDelegate(typeof(EncodeSymbolHandler));

            _getDescriptionAsyncMethod = (GetDescriptionAsyncHandler)_internalType
                .GetMethod("GetDescriptionAsync", new[] { typeof(CompletionItem), typeof(Document), typeof(CancellationToken) })
                .CreateDelegate(typeof(GetDescriptionAsyncHandler));
        }

        public static Task<CompletionDescription> GetDescriptionAsync(CompletionItem item, Document document, CancellationToken cancellationToken) => _getDescriptionAsyncMethod(item, document, cancellationToken);

        public static string EncodeSymbol(ISymbol symbol) => _encodeSymbolMethod(symbol);
    }
}