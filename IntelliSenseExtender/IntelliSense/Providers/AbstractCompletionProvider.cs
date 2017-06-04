using System.Collections.Generic;
using System.Linq;
using IntelliSenseExtender.Editor;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public abstract class AbstractCompletionProvider : CompletionProvider
    {
        protected readonly NamespaceResolver _namespaceResolver;
        protected readonly IOptionsProvider _optionsProvider;

        public AbstractCompletionProvider()
            : this(VsSettingsOptionsProvider.Current)
        {
        }

        public AbstractCompletionProvider(IOptionsProvider optionsProvider)
        {
            _optionsProvider = optionsProvider;
            _namespaceResolver = new NamespaceResolver();
        }

        public Options.Options Options => _optionsProvider.GetOptions();

        protected List<INamedTypeSymbol> GetAllTypes(SyntaxContext context)
        {
            const int typesCapacity = 100000;

            var foundTypes = new List<INamedTypeSymbol>(typesCapacity);

            var namespacesToTraverse = new[] { context.SemanticModel.Compilation.GlobalNamespace };
            while (namespacesToTraverse.Length > 0)
            {
                var members = namespacesToTraverse.SelectMany(ns => ns.GetMembers()).ToArray();
                var typeSymbols = members
                    .OfType<INamedTypeSymbol>()
                    .Where(symbol => FilterType(symbol, context));
                foundTypes.AddRange(typeSymbols);
                namespacesToTraverse = members
                    .OfType<INamespaceSymbol>()
                    .Where(FilterNamespace)
                    .ToArray();
            }

            return FilterOutObsoleteSymbolsIfNeeded(foundTypes);
        }

        protected virtual bool FilterType(INamedTypeSymbol type, SyntaxContext syntaxContext)
        {
            return (type.DeclaredAccessibility == Accessibility.Public
                    || (type.DeclaredAccessibility == Accessibility.Internal
                        && type.ContainingAssembly == syntaxContext.SemanticModel.Compilation.Assembly))
                && type.CanBeReferencedByName;
        }

        protected List<T> FilterOutObsoleteSymbolsIfNeeded<T>(IEnumerable<T> symbolsList) where T : ISymbol
        {
            return Options.FilterOutObsoleteSymbols
                ? symbolsList.Where(symbol => !symbol.IsObsolete()).ToList()
                : symbolsList.ToList();
        }

        private bool FilterNamespace(INamespaceSymbol ns)
        {
            bool userCodeOnly = Options.UserCodeOnlySuggestions;
            return (!userCodeOnly || ns.Locations.Any(l => l.IsInSource))
                 && ns.CanBeReferencedByName;
        }
    }
}
