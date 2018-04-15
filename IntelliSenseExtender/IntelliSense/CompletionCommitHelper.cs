using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.Editor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

namespace IntelliSenseExtender.IntelliSense
{
    public static class CompletionCommitHelper
    {
        private static readonly NamespaceResolver _namespaceResolver = new NamespaceResolver();

        public static async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            if (!item.Properties.TryGetValue(CompletionItemProperties.InsertText, out string insertText))
            {
                insertText = item.DisplayText;
            }

            int? newPosition = null;
            if (item.Properties.TryGetValue(CompletionItemProperties.NewPositionOffset, out string positionOffsetString)
                && int.TryParse(positionOffsetString, out int positionOffset)
                && positionOffset != 0)
            {
                int originalNewPosition = item.Span.End + insertText.Length;
                newPosition = originalNewPosition + positionOffset;
            }

            // Add using for required symbol. 
            // Any better place to put this?
            if (item.Properties.TryGetValue(CompletionItemProperties.Unimported, out string unimportedString)
                && bool.Parse(unimportedString)
                && item.Properties.TryGetValue(CompletionItemProperties.Namespace, out string nsName)
                && IsCommitContext())
            {
                await _namespaceResolver.AddNamespaceAndApplyAsync(nsName, document, cancellationToken).ConfigureAwait(false);
            }

            return CompletionChange.Create(new TextChange(item.Span, insertText), newPosition);
        }

        private static bool IsCommitContext()
        {
            // GetChangeAsync is called not only before actual commit (e.g. in SpellCheck as well).
            // Manual adding 'using' in that method causes random adding usings.
            // To avoid that we verify that we are actually committing item.
            // TODO: PLEASE FIND BETTER APPROACH!!!

            var stacktrace = new StackTrace();

            bool isCommitContext = stacktrace.GetFrames()
                .Select(frame => frame.GetMethod())
                .Any(method => method.Name == "Commit" && method.DeclaringType.Name == "Controller");

            return isCommitContext;
        }
    }
}
