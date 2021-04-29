using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.ExposedInternals;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.IntelliSense.Context;
using Microsoft.CodeAnalysis;

namespace IntelliSenseExtender.Context
{
    public class SyntaxContext
    {
        private readonly NamespacesTree _importedNamespacesTree;

        public Document Document { get; }
        public SemanticModel SemanticModel { get; }
        public SyntaxTree SyntaxTree { get; }
        public int Position { get; }

        public ImmutableHashSet<INamespaceSymbol> ImportedNamespaces { get; }
        public ImmutableHashSet<ITypeSymbol> StaticImports { get; }
        public ImmutableDictionary<INamespaceOrTypeSymbol, string> Aliases { get; }

        public bool IsTypeContext { get; }
        public bool IsAttributeContext { get; }

        public InferredTypeInfo InferredInfo { get; }

        public SyntaxToken CurrentToken { get; }
        public CancellationToken CancellationToken { get; }

        public SyntaxContext(Document document, SemanticModel semanticModel, SyntaxTree syntaxTree, int position,
            ImmutableHashSet<INamespaceSymbol> importedNamespaces, ImmutableHashSet<ITypeSymbol> staticImports, ImmutableDictionary<INamespaceOrTypeSymbol, string> aliases,
            bool isTypeContext, bool isAttributeContext,
            InferredTypeInfo inferredTypeInfo,
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

            InferredInfo = inferredTypeInfo;

            CurrentToken = currentToken;
            CancellationToken = token;

            _importedNamespacesTree = new NamespacesTree(ImportedNamespaces);
        }

        public bool IsAccessible(ISymbol symbol)
        {
            return symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => true,

                Accessibility.Internal => symbol.ContainingAssembly?.Name == Document.Project.AssemblyName,

                _ => false,
            };
        }

        public bool IsNamespaceImported(INamespaceSymbol nsSymbol)
        {
            return _importedNamespacesTree.Contains(nsSymbol);
        }

        public bool IsNamespaceImported(string nsName)
        {
            return _importedNamespacesTree.Contains(nsName);
        }

        public static async Task<SyntaxContext?> CreateAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            if (semanticModel == null || syntaxTree == null)
                return null;

            var isTypeContext = syntaxTree.IsTypeContext(position, cancellationToken, semanticModel);
            var isAttributeContext = isTypeContext && syntaxTree.IsAttributeNameContext(position, cancellationToken);

            var currentToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var (importedNamespaces, staticImports, aliases) = semanticModel.GetUsings(currentToken);

            var inferredTypeInfo = semanticModel.GetTypeSymbol(currentToken);

            return new SyntaxContext(document, semanticModel, syntaxTree, position,
                importedNamespaces, staticImports, aliases,
                isTypeContext, isAttributeContext,
                inferredTypeInfo,
                currentToken, cancellationToken);
        }

        /// <summary>
        /// Build tree of namespaces to optimize verification for imported namespace.
        /// (Cannot compare INamespaceSymbols directly, as there might be different symbols - merged / unmerged namespaces)
        /// </summary>
        private class NamespacesTree : Dictionary<string, NamespacesTree>
        {
            public NamespacesTree(IEnumerable<INamespaceSymbol> namespaces)
            {
                var nsGroups = namespaces
                    .Where(ns => !ns.IsGlobalNamespace)
                    .GroupBy(ns => ns.Name);

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
                    if (!treeLevel.TryGetValue(currentNs.Name, out var nextLevel))
                        return false;

                    treeLevel = nextLevel;
                    currentNs = currentNs.ContainingNamespace;
                }

                return true;
            }

            public bool Contains(string nsName)
            {
                var parts = nsName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                var current = this;

                for (int i = parts.Length - 1; i >= 0; i--)
                {
                    string part = parts[i];
                    if (!current.TryGetValue(part, out current!))
                        return false;
                }

                return true;
            }
        }
    }
}
