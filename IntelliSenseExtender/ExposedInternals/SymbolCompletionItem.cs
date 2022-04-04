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
        private static readonly GetDescriptionAsyncOldHandler _getDescriptionAsyncOldMethod;

        private static readonly MethodInfo _getDescriptionAsyncMethod;

        private static object _symbolDescriptionOptionsDefault;

        private delegate string EncodeSymbolHandler(ISymbol symbol);
        private delegate Task<CompletionDescription> GetDescriptionAsyncOldHandler(CompletionItem item, Document document, CancellationToken cancellationToken);

        static SymbolCompletionItem()
        {
            var featuresAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .First(a => a.GetName().Name == "Microsoft.CodeAnalysis.Features");
            var symbolCompletionItemType = featuresAssembly.GetType("Microsoft.CodeAnalysis.Completion.Providers.SymbolCompletionItem");

            _encodeSymbolMethod = (EncodeSymbolHandler)symbolCompletionItemType
                .GetMethod("EncodeSymbol", new[] { typeof(ISymbol) })
                .CreateDelegate(typeof(EncodeSymbolHandler));

            // Pre-17.2 P1
            var getDescriptionOldMethod = symbolCompletionItemType
                .GetMethod("GetDescriptionAsync", new[] { typeof(CompletionItem), typeof(Document), typeof(CancellationToken) });

            if (getDescriptionOldMethod != null)
            {
                _getDescriptionAsyncOldMethod = (GetDescriptionAsyncOldHandler)getDescriptionOldMethod.CreateDelegate(typeof(GetDescriptionAsyncOldHandler));
            }
            else
            {
                var symbolDescriptionOptionsType = featuresAssembly.GetType("Microsoft.CodeAnalysis.LanguageServices.SymbolDescriptionOptions");
                _symbolDescriptionOptionsDefault = symbolDescriptionOptionsType.GetField("Default").GetValue(null);
                _getDescriptionAsyncMethod = symbolCompletionItemType.GetMethod("GetDescriptionAsync", new[] { typeof(CompletionItem), typeof(Document), symbolDescriptionOptionsType, typeof(CancellationToken) });
            }
        }

        public static Task<CompletionDescription> GetDescriptionAsync(CompletionItem item, Document document, CancellationToken cancellationToken)
        {
            if (_getDescriptionAsyncOldMethod != null)
            {
                return _getDescriptionAsyncOldMethod(item, document, cancellationToken);
            }
            else
            {
                return (Task<CompletionDescription>)_getDescriptionAsyncMethod.Invoke(null, new object[] { item, document, _symbolDescriptionOptionsDefault, cancellationToken });
            }
        }

        public static string EncodeSymbol(ISymbol symbol) => _encodeSymbolMethod(symbol);
    }
}