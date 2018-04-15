using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.ExposedInternals;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.IntelliSense.Context;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntelliSenseExtender.Context
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
        public ITypeSymbol AccessedSymbolType { get; }
        public ISymbol AccessedSymbol { get; }

        public ITypeSymbol InferredType { get; }
        public TypeInferredFrom TypeInferredFrom { get; }

        public SyntaxToken CurrentToken { get; }
        public CancellationToken CancellationToken { get; }

        public SyntaxContext(Document document, SemanticModel semanticModel, SyntaxTree syntaxTree, int position,
            IReadOnlyList<string> importedNamespaces, bool isTypeContext = false, bool isAttributeContext = false,
            bool isMemberAccessContext = false, ITypeSymbol accessedSymbolType = null, ISymbol accessedSymbol = null,
            ITypeSymbol inferredType = null, TypeInferredFrom inferredFrom = TypeInferredFrom.None,
            SyntaxToken currentToken = default(SyntaxToken), CancellationToken token = default(CancellationToken))
        {
            Document = document;
            SemanticModel = semanticModel;
            SyntaxTree = syntaxTree;
            Position = position;
            ImportedNamespaces = importedNamespaces;
            IsTypeContext = isTypeContext;
            IsAttributeContext = isAttributeContext;
            IsMemberAccessContext = isMemberAccessContext;
            AccessedSymbolType = accessedSymbolType;
            AccessedSymbol = accessedSymbol;
            InferredType = inferredType;
            TypeInferredFrom = inferredFrom;

            CurrentToken = currentToken;
            CancellationToken = token;
        }

        public bool IsNamespaceImported(INamedTypeSymbol typeSymbol)
        {
            return ImportedNamespaces.Contains(typeSymbol.GetNamespace())
                && !typeSymbol.ContainingNamespace.IsGlobalNamespace;
        }

        public bool IsAccessible(ISymbol typeSymbol)
        {
            //TODO: add support for internal's
            return typeSymbol.DeclaredAccessibility == Accessibility.Public;
        }

        public static async Task<SyntaxContext> CreateAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
            var syntaxTree = await document.GetSyntaxTreeAsync().ConfigureAwait(false);

            var importedNamespaces = syntaxTree.GetImportedNamespaces();
            var isTypeContext = syntaxTree.IsTypeContext(position, cancellationToken, semanticModel);
            var isAttributeContext = isTypeContext && syntaxTree.IsAttributeNameContext(position, cancellationToken);

            var currentToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

            ExpressionSyntax accessedSyntax = null;
            bool isMemberAccessContext = !isTypeContext && currentToken.IsMemberAccessContext(out accessedSyntax);
            ITypeSymbol accessedTypeSymbol = null;
            ISymbol accessedSymbol = null;
            if (isMemberAccessContext)
            {
                accessedTypeSymbol = semanticModel.GetTypeInfo(accessedSyntax, cancellationToken).Type;
                accessedSymbol = semanticModel.GetSymbolInfo(accessedSyntax).Symbol;
            }

            var (inferredType, inferredFrom) = semanticModel.GetTypeSymbol(currentToken);

            return new SyntaxContext(document, semanticModel, syntaxTree, position,
                importedNamespaces, isTypeContext, isAttributeContext, isMemberAccessContext,
                accessedTypeSymbol, accessedSymbol,
                inferredType, inferredFrom,
                currentToken, cancellationToken);
        }
    }
}
