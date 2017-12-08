using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.Editor;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    [ExportCompletionProvider("Unimported provider", LanguageNames.CSharp)]
    public class UnimportedCSharpCompletionProvider : CompletionProvider
    {
        private readonly NamespaceResolver _namespaceResolver;
        private readonly IOptionsProvider _optionsProvider;
        private Dictionary<string, ISymbol> _symbolMapping;

        public UnimportedCSharpCompletionProvider()
            : this(VsSettingsOptionsProvider.Current)
        {
        }

        public UnimportedCSharpCompletionProvider(IOptionsProvider optionsProvider)
        {
            _optionsProvider = optionsProvider;
            _namespaceResolver = new NamespaceResolver();
        }

        public Options.Options Options => _optionsProvider.GetOptions();

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            if (Options.EnableUnimportedSuggestions)
            {
                _symbolMapping = new Dictionary<string, ISymbol>();
                var syntaxContext = await SyntaxContext.Create(context.Document, context.Position, context.CancellationToken)
                    .ConfigureAwait(false);

                var symbols = GetSymbols(syntaxContext);
                var completionItemsToAdd = symbols
                    .Select(symbol => CreateCompletionItemForSymbol(symbol, syntaxContext))
                    .ToList();

                context.AddItems(completionItemsToAdd);
            }
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            return TryGetSymbolMapping(item, out ISymbol symbol)
                ? CompletionItemHelper.GetUnimportedDescriptionAsync(document, item, symbol, cancellationToken)
                : base.GetDescriptionAsync(document, item, cancellationToken);
        }

        public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        {
            string insertText;
            if (!item.Properties.TryGetValue(CompletionItemProperties.InsertText, out insertText))
            {
                insertText = item.DisplayText;
            }
            var change = Task.FromResult(CompletionChange.Create(new TextChange(item.Span, insertText)));

            // Add using for required symbol. 
            // Any better place to put this?
            if (TryGetSymbolMapping(item, out ISymbol symbol) && IsCommitContext())
            {
                _namespaceResolver.AddNamespace(symbol.GetNamespace());
            }

            return change;
        }

        private CompletionItem CreateCompletionItemForSymbol(ISymbol typeSymbol, SyntaxContext context)
        {
            bool sortLast = Options.SortCompletionsAfterImported;
            var completionItem = CompletionItemHelper.CreateCompletionItem(typeSymbol, context, sortLast);

            var fullSymbolName = completionItem.Properties[CompletionItemProperties.FullSymbolName];
            _symbolMapping[fullSymbolName] = typeSymbol;

            return completionItem;
        }

        private async Task<Document> AddUsings(Document document, params string[] nameSpaces)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            if (syntaxRoot is CompilationUnitSyntax compilationUnitSyntax)
            {
                var usingNames = nameSpaces
                    .Select(ns => SyntaxFactory.ParseName(ns))
                    .Select(nsName => SyntaxFactory.UsingDirective(nsName).NormalizeWhitespace())
                    .ToArray();
                compilationUnitSyntax = compilationUnitSyntax.AddUsings(usingNames);
                return document.WithSyntaxRoot(compilationUnitSyntax);
            }
            else
            {
                return document;
            }
        }

        private IEnumerable<ISymbol> GetSymbols(SyntaxContext context)
        {
            if (Options.EnableTypesSuggestions && context.IsTypeContext)
            {
                var typeSymbols = GetAllTypes(context);
                if (context.IsAttributeContext)
                {
                    typeSymbols = FilterAttributes(typeSymbols);
                }
                return typeSymbols;
            }
            else if (Options.EnableExtensionMethodsSuggestions
                && context.IsMemberAccessContext
                && context.AccessedSymbol?.Kind != SymbolKind.NamedType
                && context.AccessedSymbolType != null)
            {
                return GetApplicableExtensionMethods(context);
            }

            return Enumerable.Empty<ISymbol>();
        }

        private bool TryGetSymbolMapping(CompletionItem item, out ISymbol symbol)
        {
            symbol = null;
            if (item.Properties.TryGetValue(CompletionItemProperties.FullSymbolName,
                out string fullSymbolName))
            {
                return _symbolMapping.TryGetValue(fullSymbolName, out symbol);
            }
            return false;
        }

        private List<INamedTypeSymbol> GetAllTypes(SyntaxContext context)
        {
            const int typesCapacity = 1000;

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

        private List<IMethodSymbol> GetApplicableExtensionMethods(SyntaxContext context)
        {
            var accessedTypeSymbol = context.AccessedSymbolType;
            var foundExtensionSymbols = GetAllTypes(context)
                .Where(type => type.MightContainExtensionMethods)
                .SelectMany(type => type.GetMembers())
                .OfType<IMethodSymbol>()
                .Select(m => m.ReduceExtensionMethod(accessedTypeSymbol))
                .Where(m => m != null)
                .ToList();

            return FilterOutObsoleteSymbolsIfNeeded(foundExtensionSymbols);
        }

        private bool FilterNamespace(INamespaceSymbol ns)
        {
            bool userCodeOnly = Options.UserCodeOnlySuggestions;
            return (!userCodeOnly || ns.Locations.Any(l => l.IsInSource))
                 && ns.CanBeReferencedByName;
        }

        private bool FilterType(INamedTypeSymbol type, SyntaxContext syntaxContext)
        {
            return (type.DeclaredAccessibility == Accessibility.Public
                    || (type.DeclaredAccessibility == Accessibility.Internal
                        && type.ContainingAssembly == syntaxContext.SemanticModel.Compilation.Assembly))
                && type.CanBeReferencedByName
                && !syntaxContext.ImportedNamespaces.Contains(type.GetNamespace());
        }

        private List<INamedTypeSymbol> FilterAttributes(IEnumerable<INamedTypeSymbol> list)
        {
            return list
                .Where(ts => ts.IsAttribute() && !ts.IsAbstract)
                .ToList();
        }

        private List<T> FilterOutObsoleteSymbolsIfNeeded<T>(IEnumerable<T> symbolsList) where T : ISymbol
        {
            return Options.FilterOutObsoleteSymbols
                ? symbolsList.Where(symbol => !symbol.IsObsolete()).ToList()
                : symbolsList.ToList();
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
