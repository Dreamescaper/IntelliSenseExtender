using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.ExposedInternals
{
    public static class CompletionItemInternal
    {
        private delegate CompletionItem CreateCompletionItem(
            string displayText,
            string filterText = null,
            string sortText = null,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default,
            CompletionItemRules rules = null,
            string displayTextPrefix = null,
            string displayTextSuffix = null,
            string inlineDescription = null);

        private static CreateCompletionItem _createMethodDelegate;


        static CompletionItemInternal()
        {
#pragma warning disable REFL019 // No member matches the type
            var createMethod = typeof(CompletionItem).GetMethod(nameof(CompletionItem.Create),
                new[] { typeof(string), typeof(string), typeof(string),
                typeof(ImmutableDictionary<string, string>),
                typeof(ImmutableArray<string>),
                typeof(CompletionItemRules),
                 typeof(string), typeof(string), typeof(string)});
#pragma warning restore REFL019 // No member matches the type

            _createMethodDelegate = (CreateCompletionItem)createMethod.CreateDelegate(typeof(CreateCompletionItem));
        }

        public static CompletionItem Create(
            string displayText,
            string filterText = null,
            string sortText = null,
            ImmutableDictionary<string, string> properties = null,
            ImmutableArray<string> tags = default,
            CompletionItemRules rules = null,
            string displayTextPrefix = null,
            string displayTextSuffix = null,
            string inlineDescription = null)
        {
            return _createMethodDelegate(displayText, filterText, sortText, properties, tags, rules, displayTextPrefix, displayTextSuffix, inlineDescription);
        }
    }
}
