using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.ExposedInternals;
using IntelliSenseExtender.Extensions;
using Microsoft.CodeAnalysis;

namespace IntelliSenseExtender.IntelliSense
{
    public class SyntaxContext
    {
        public Document Document { get; }
        public SemanticModel SemanticModel { get; }
        public SyntaxTree SyntaxTree { get; }
        public int Position { get; }

        public IReadOnlyList<string> ImportedNamespaces { get; }
        public bool IsTypeContext { get; }
        public bool IsAttributeContext { get; }

        public SyntaxContext(Document document, SemanticModel semanticModel, SyntaxTree syntaxTree, int position,
            IReadOnlyList<string> importedNamespaces, bool isTypeContext, bool isAttributeContext)
        {
            Document = document;
            SemanticModel = semanticModel;
            SyntaxTree = syntaxTree;
            Position = position;
            ImportedNamespaces = importedNamespaces;
            IsTypeContext = isTypeContext;
            IsAttributeContext = isAttributeContext;
        }

        public static async Task<SyntaxContext> Create(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
            var syntaxTree = await document.GetSyntaxTreeAsync().ConfigureAwait(false);

            var importedNamespaces = syntaxTree.GetImportedNamespaces();
            var isTypeContext = syntaxTree.IsTypeContext(position, cancellationToken, semanticModel);
            var isAttributeContext = syntaxTree.IsAttributeNameContext(position, cancellationToken);

            return new SyntaxContext(document, semanticModel, syntaxTree, position,
                importedNamespaces, isTypeContext, isAttributeContext);
        }
    }
}
