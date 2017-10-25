using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.ExposedInternals;
using IntelliSenseExtender.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
        public IReadOnlyList<string> ImportedStatics { get; }
        public bool IsTypeContext { get; }
        public bool IsAttributeContext { get; }
        public bool IsMemberAccessContext { get; }
        public ITypeSymbol AccessedSymbolType { get; }
        public ISymbol AccessedSymbol { get; }
        public CancellationToken CancellationToken { get; }

        public SyntaxContext(Document document, SemanticModel semanticModel, SyntaxTree syntaxTree, int position,
            IReadOnlyList<string> importedNamespaces,
            IReadOnlyList<string> importedStatics,
            bool isTypeContext = false, bool isAttributeContext = false,
            bool isMemberAccessContext = false, ITypeSymbol accessedTypeSymbol = null, ISymbol accessedSymbol = null,
            CancellationToken token = default(CancellationToken))
        {
            Document = document;
            SemanticModel = semanticModel;
            SyntaxTree = syntaxTree;
            Position = position;
            ImportedNamespaces = importedNamespaces;
            ImportedStatics = importedStatics;
            IsTypeContext = isTypeContext;
            IsAttributeContext = isAttributeContext;
            IsMemberAccessContext = isMemberAccessContext;
            AccessedSymbolType = accessedTypeSymbol;
            AccessedSymbol = accessedSymbol;
            CancellationToken = token;
        }

        public static async Task<SyntaxContext> Create(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
            var syntaxTree = await document.GetSyntaxTreeAsync().ConfigureAwait(false);

            var importedNamespaces = syntaxTree.GetImportedNamespaces();
            var importedStatics = syntaxTree.GetImportedStatics();
            var isTypeContext = syntaxTree.IsTypeContext(position, cancellationToken, semanticModel);
            var isAttributeContext = isTypeContext && syntaxTree.IsAttributeNameContext(position, cancellationToken);

            ExpressionSyntax accessedSyntax = null;
            bool isMemberAccessContext = !isTypeContext
                && syntaxTree.IsMemberAccessContext(position, out accessedSyntax, cancellationToken);
            ITypeSymbol accessedTypeSymbol = null;
            ISymbol accessedSymbol = null;
            if (isMemberAccessContext)
            {
                accessedTypeSymbol = semanticModel.GetTypeInfo(accessedSyntax, cancellationToken).Type;
                accessedSymbol = semanticModel.GetSymbolInfo(accessedSyntax).Symbol;
            }

            return new SyntaxContext(document, semanticModel, syntaxTree, position,
                importedNamespaces, importedStatics, isTypeContext, isAttributeContext, isMemberAccessContext,
                accessedTypeSymbol, accessedSymbol, cancellationToken);
        }
    }
}
