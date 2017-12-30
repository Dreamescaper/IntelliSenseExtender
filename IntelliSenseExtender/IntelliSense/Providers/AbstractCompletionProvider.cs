using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.Editor;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public abstract class AbstractCompletionProvider : CompletionProvider
    {
        protected readonly NamespaceResolver _namespaceResolver;
        protected readonly IOptionsProvider _optionsProvider;

        protected AbstractCompletionProvider()
            : this(VsSettingsOptionsProvider.Current)
        {
        }

        protected AbstractCompletionProvider(IOptionsProvider optionsProvider)
        {
            _optionsProvider = optionsProvider;
            _namespaceResolver = new NamespaceResolver();
        }

        public Options.Options Options => _optionsProvider.GetOptions();

        public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        {
            string insertText;
            if (!item.Properties.TryGetValue(CompletionItemProperties.InsertText, out insertText))
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
                && TryGetItemSymbolMapping(item, document, out ISymbol symbol)
                && IsCommitContext())
            {
                _namespaceResolver.AddNamespace(symbol.GetNamespace());
            }

            return Task.FromResult(CompletionChange.Create(new TextChange(item.Span, insertText), newPosition));
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            return TryGetItemSymbolMapping(item, document, out ISymbol symbol)
                ? CompletionItemHelper.GetUnimportedDescriptionAsync(document, item, symbol, cancellationToken)
                : base.GetDescriptionAsync(document, item, cancellationToken);
        }

        protected IEnumerable<INamedTypeSymbol> GetAllTypes(SyntaxContext context)
        {
            IEnumerable<INamedTypeSymbol> getAllTypes(Compilation compilation)
            {
                var namespacesToTraverse = new List<INamespaceSymbol> { compilation.GlobalNamespace };
                while (namespacesToTraverse.Count > 0)
                {
                    var members = namespacesToTraverse.SelectMany(ns => ns.GetMembers());
                    namespacesToTraverse = new List<INamespaceSymbol>();

                    foreach (var member in members)
                    {
                        if (member is INamedTypeSymbol namedTypeSymbol
                            && FilterType(namedTypeSymbol, context))
                        {
                            yield return namedTypeSymbol;
                        }
                        else if (member is INamespaceSymbol namespaceSymbol
                            && FilterNamespace(namespaceSymbol))
                        {
                            namespacesToTraverse.Add(namespaceSymbol);
                        }
                    }
                }
            }

            var allTypes = getAllTypes(context.SemanticModel.Compilation);
            return FilterOutObsoleteSymbolsIfNeeded(allTypes);
        }

        protected virtual bool FilterType(INamedTypeSymbol type, SyntaxContext syntaxContext)
        {
            return (type.DeclaredAccessibility == Accessibility.Public
                    || (type.DeclaredAccessibility == Accessibility.Internal
                        && type.ContainingAssembly == syntaxContext.SemanticModel.Compilation.Assembly))
                && type.CanBeReferencedByName;
        }

        protected IEnumerable<T> FilterOutObsoleteSymbolsIfNeeded<T>(IEnumerable<T> symbols) where T : ISymbol
        {
            return Options.FilterOutObsoleteSymbols
                ? symbols.Where(symbol => !symbol.IsObsolete())
                : symbols;
        }

        private static (Document document, Dictionary<string, ISymbol> mapping) _symbolMappingCache;
        private static Dictionary<string, ISymbol> GetSymbolMapping(Document currentDocument)
        {
            if (_symbolMappingCache.document?.Id != currentDocument.Id)
            {
                _symbolMappingCache.document = currentDocument;
                _symbolMappingCache.mapping = new Dictionary<string, ISymbol>();
            }
            return _symbolMappingCache.mapping;
        }

        protected static void AddSymbolToMapping(Document currentDocument, CompletionItem item, ISymbol symbol)
        {
            if (item.Properties.TryGetValue(CompletionItemProperties.FullSymbolName,
                out string fullSymbolName))
            {
                GetSymbolMapping(currentDocument)[fullSymbolName] = symbol;
            }
        }

        protected static bool TryGetItemSymbolMapping(CompletionItem item, Document currentDocument, out ISymbol symbol)
        {
            symbol = null;
            if (item.Properties.TryGetValue(CompletionItemProperties.FullSymbolName,
                out string fullSymbolName))
            {
                return GetSymbolMapping(currentDocument).TryGetValue(fullSymbolName, out symbol);
            }
            return false;
        }

        private bool FilterNamespace(INamespaceSymbol ns)
        {
            bool userCodeOnly = Options.UserCodeOnlySuggestions;
            return (!userCodeOnly || ns.Locations.Any(l => l.IsInSource))
                 && ns.CanBeReferencedByName;
        }

        private bool IsCommitContext()
        {
            // GetChangeAsync is called not only before actual commit (e.g. in SpellCheck as well).
            // Manual adding 'using' in that method causes random adding usings.
            // To avoid that we verify that we are actually committing item.
            // TODO: PLEASE FIND BETTER APPROACH!!!

            var stacktrace = new StackTrace();
            var frames = stacktrace.GetFrames();
            bool isCommitContext = frames
                .Select(frame => frame.GetMethod())
                .Any(method => method.Name == "Commit" && method.DeclaringType.Name == "Controller");

            return isCommitContext;
        }
    }
}
