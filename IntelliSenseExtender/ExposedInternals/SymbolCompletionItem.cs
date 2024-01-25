using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

#nullable disable

namespace IntelliSenseExtender.ExposedInternals
{
    /// <summary>
    /// Exposing some methods from internal class
    /// <see cref="Microsoft.CodeAnalysis.Completion.Providers.SymbolCompletionItem"/>
    /// </summary>
    public static class SymbolCompletionItem
    {
        private static readonly EncodeSymbolHandler _encodeSymbolMethod;

        private static readonly MethodInfo _getDescriptionAsyncMethod;

        private static object _symbolDescriptionOptionsDefault;

        private delegate string EncodeSymbolHandler(ISymbol symbol);

        static SymbolCompletionItem()
        {
            var featuresAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .First(a => a.GetName().Name == "Microsoft.CodeAnalysis.Features");
            var symbolCompletionItemType = featuresAssembly.GetType("Microsoft.CodeAnalysis.Completion.Providers.SymbolCompletionItem");

            _encodeSymbolMethod = (EncodeSymbolHandler)symbolCompletionItemType
                .GetMethod("EncodeSymbol", new[] { typeof(ISymbol) })
                .CreateDelegate(typeof(EncodeSymbolHandler));

            var symbolDescriptionOptionsType = featuresAssembly.GetType("Microsoft.CodeAnalysis.LanguageService.SymbolDescriptionOptions");
            _symbolDescriptionOptionsDefault = symbolDescriptionOptionsType.GetField("Default").GetValue(null);
            _getDescriptionAsyncMethod = symbolCompletionItemType.GetMethod("GetDescriptionAsync", new[] { typeof(CompletionItem), typeof(Document), symbolDescriptionOptionsType, typeof(CancellationToken) });
        }

        public static Task<CompletionDescription> GetDescriptionAsync(CompletionItem item, Document document, CancellationToken cancellationToken)
        {
            return (Task<CompletionDescription>)_getDescriptionAsyncMethod.Invoke(null, [item, document, _symbolDescriptionOptionsDefault, cancellationToken]);
        }

        public static string EncodeSymbol(ISymbol symbol) => _encodeSymbolMethod(symbol);
    }
}