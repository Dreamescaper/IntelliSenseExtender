using Microsoft.CodeAnalysis;

namespace IntelliSenseExtender.IntelliSense
{
    public static class CompletionItemHelper
    {
        public static string GetDisplayText(ISymbol symbol, SyntaxContext context)
        {
            const string AttributeSuffix = "Attribute";

            var symbolText = symbol.Name;
            if (context.IsAttributeContext
                && symbolText.EndsWith(AttributeSuffix))
            {
                symbolText = symbolText.Substring(0, symbolText.Length - AttributeSuffix.Length);
            }

            return symbolText;
        }
    }
}
