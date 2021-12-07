using System.Collections.Generic;
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
            var insertText = item.DisplayText;

            int? newPosition = null;
            if (item.Properties.TryGetValue(CompletionItemProperties.NewPositionOffset, out var positionOffsetString)
                && int.TryParse(positionOffsetString, out int positionOffset)
                && positionOffset != 0)
            {
                int originalNewPosition = item.Span.End + insertText.Length;
                newPosition = originalNewPosition + positionOffset;
            }

            var textChange = new TextChange(item.Span, insertText);

            // Create TextChange with added using
            if (item.Properties.TryGetValue(CompletionItemProperties.NamespaceToImport, out var nsName))
            {
                int position = item.Span.End;
                var sourceTextTask = document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var docWithUsing = await _namespaceResolver.AddNamespaceImportAsync(nsName, document, position, cancellationToken).ConfigureAwait(false);
                var usingChange = await docWithUsing.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);

                var changes = usingChange.Union(new[] { textChange }).ToList();
                var sourceText = await sourceTextTask;
                sourceText = sourceText.WithChanges(changes);

                textChange = Collapse(sourceText, changes);
            }

            return CompletionChange.Create(textChange, newPosition);
        }

        // Taken from
        private static TextChange Collapse(SourceText newText, List<TextChange> changes)
        {
            if (changes.Count == 0)
            {
                return new TextChange(new TextSpan(0, 0), "");
            }
            else if (changes.Count == 1)
            {
                return changes[0];
            }

            // The span we want to replace goes from the start of the first span to the end of
            // the  last span.
            var totalOldSpan = TextSpan.FromBounds(changes.Select(s => s.Span.Start).Min(), changes.Select(s => s.Span.End).Max());

            // We figure out the text we're replacing with by actually just figuring out the
            // new span in the newText and grabbing the text out of that.  The newSpan will
            // start from the same position as the oldSpan, but it's length will be the old
            // span's length + all the deltas we accumulate through each text change.  i.e.
            // if the first change adds 2 characters and the second change adds 4, then 
            // the newSpan will be 2+4=6 characters longer than the old span.
            var sumOfDeltas = changes.Sum(c => c.NewText is null ? 0 : c.NewText.Length - c.Span.Length);
            var totalNewSpan = new TextSpan(totalOldSpan.Start, totalOldSpan.Length + sumOfDeltas);

            return new TextChange(totalOldSpan, newText.ToString(totalNewSpan));
        }
    }
}
