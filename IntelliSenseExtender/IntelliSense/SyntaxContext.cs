using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.ExposedInternals;
using IntelliSenseExtender.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        public bool IsMemberAccessContext { get; }
        public ITypeSymbol AccessedTypeSymbol { get; }
        public CancellationToken CancellationToken { get; }

        public SyntaxContext(Document document, SemanticModel semanticModel, SyntaxTree syntaxTree, int position,
            IReadOnlyList<string> importedNamespaces, bool isTypeContext = false, bool isAttributeContext = false,
            bool isMemberAccessContext = false, ITypeSymbol accessedTypeSymbol = null, CancellationToken token = default(CancellationToken))
        {
            Document = document;
            SemanticModel = semanticModel;
            SyntaxTree = syntaxTree;
            Position = position;
            ImportedNamespaces = importedNamespaces;
            IsTypeContext = isTypeContext;
            IsAttributeContext = isAttributeContext;
            IsMemberAccessContext = isMemberAccessContext;
            AccessedTypeSymbol = accessedTypeSymbol;
            CancellationToken = token;
        }

        public static async Task<SyntaxContext> Create(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
            var syntaxTree = await document.GetSyntaxTreeAsync().ConfigureAwait(false);

            var importedNamespaces = syntaxTree.GetImportedNamespaces();
            var isTypeContext = syntaxTree.IsTypeContext(position, cancellationToken, semanticModel);
            var isAttributeContext = syntaxTree.IsAttributeNameContext(position, cancellationToken);

            var isMemberAccessContext = syntaxTree.IsMemberAccessContext(position, out ExpressionSyntax accessedSyntax, cancellationToken);
            ITypeSymbol accessedTypeSymbol = accessedSyntax == null
                ? null
                : semanticModel.GetTypeInfo(accessedSyntax, cancellationToken).Type;

            return new SyntaxContext(document, semanticModel, syntaxTree, position,
                importedNamespaces, isTypeContext, isAttributeContext, isMemberAccessContext, accessedTypeSymbol, cancellationToken);
        }
    }
}
