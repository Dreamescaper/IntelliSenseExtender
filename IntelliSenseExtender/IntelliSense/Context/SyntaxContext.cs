using System.Collections.Generic;
using System.Collections.Immutable;
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
        private NamespacesTree _importedNamespacesTree;

        public Document Document { get; }
        public SemanticModel SemanticModel { get; }
        public SyntaxTree SyntaxTree { get; }
        public int Position { get; }

        public ImmutableHashSet<INamespaceSymbol> ImportedNamespaces { get; }
        public ImmutableHashSet<ITypeSymbol> StaticImports { get; }
        public ImmutableDictionary<INamespaceOrTypeSymbol, string> Aliases { get; }

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
            ImmutableHashSet<INamespaceSymbol> importedNamespaces, ImmutableHashSet<ITypeSymbol> staticImports, ImmutableDictionary<INamespaceOrTypeSymbol, string> aliases,
            bool isTypeContext = false, bool isAttributeContext = false,
            bool isMemberAccessContext = false, ITypeSymbol accessedSymbolType = null, ISymbol accessedSymbol = null,
            ITypeSymbol inferredType = null, TypeInferredFrom inferredFrom = TypeInferredFrom.None,
            SyntaxToken currentToken = default, CancellationToken token = default)
        {
            Document = document;
            SemanticModel = semanticModel;
            SyntaxTree = syntaxTree;
            Position = position;

            ImportedNamespaces = importedNamespaces;
            StaticImports = staticImports;
            Aliases = aliases;

            IsTypeContext = isTypeContext;
            IsAttributeContext = isAttributeContext;
            IsMemberAccessContext = isMemberAccessContext;
            AccessedSymbolType = accessedSymbolType;
            AccessedSymbol = accessedSymbol;
            InferredType = inferredType;
            TypeInferredFrom = inferredFrom;

            CurrentToken = currentToken;
            CancellationToken = token;

            _importedNamespacesTree = new NamespacesTree(ImportedNamespaces);
        }

        public bool IsAccessible(ISymbol typeSymbol)
        {
            //TODO: add support for internal's
            return typeSymbol.DeclaredAccessibility == Accessibility.Public;
        }

        public bool IsNamespaceImported(INamespaceSymbol nsSymbol)
        {
            return _importedNamespacesTree.Contains(nsSymbol);
        }

        public static async Task<SyntaxContext> CreateAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            var isTypeContext = syntaxTree.IsTypeContext(position, cancellationToken, semanticModel);
            var isAttributeContext = isTypeContext && syntaxTree.IsAttributeNameContext(position, cancellationToken);

            var currentToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var (importedNamespaces, staticImports, aliases) = semanticModel.GetUsings(currentToken);

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
                importedNamespaces, staticImports, aliases,
                isTypeContext, isAttributeContext, isMemberAccessContext,
                accessedTypeSymbol, accessedSymbol,
                inferredType, inferredFrom,
                currentToken, cancellationToken);
        }

        /// <summary>
        /// Build tree of namespaces to optimize verification for imported namespace
        /// </summary>
        private class NamespacesTree : Dictionary<string, NamespacesTree>
        {
            public NamespacesTree(IEnumerable<INamespaceSymbol> namespaces)
            {
                var nsGroups = namespaces.GroupBy(ns => ns.Name);
                foreach (var nsGroup in nsGroups)
                {
                    var parentNs = nsGroup
                        .Select(g => g.ContainingNamespace)
                        .Where(ns => ns?.IsGlobalNamespace == false);

                    this[nsGroup.Key] = new NamespacesTree(parentNs);
                }
            }

            public bool Contains(INamespaceSymbol nsSymbol)
            {
                var currentNs = nsSymbol;
                var treeLevel = this;

                while (currentNs?.IsGlobalNamespace == false)
                {
                    if (!treeLevel.TryGetValue(currentNs.Name, out treeLevel))
                        return false;

                    currentNs = currentNs.ContainingNamespace;
                }

                return true;
            }
        }
    }
}
