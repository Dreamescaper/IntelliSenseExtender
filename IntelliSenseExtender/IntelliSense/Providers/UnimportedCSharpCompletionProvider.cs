using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.Editor;
using IntelliSenseExtender.ExposedInternals;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    [ExportCompletionProvider("Unimported provider", LanguageNames.CSharp)]
    public class UnimportedCSharpCompletionProvider : CompletionProvider
    {
        private const string NamespaceProperty = "Namespace";
        private const string SymbolsProperty = "Symbols";
        private const string SymbolNameProperty = "SymbolName";
        private const string SymbolKindProperty = "SymbolKind";
        private const string SymbolIndexProperty = "SymbolIndex";
        private const string ContextPositionProperty = "ContextPosition";

        private NamespaceResolver _namespaceResolver;
        private List<ISymbol> _symbolMapping;

        public UnimportedCSharpCompletionProvider()
        {
            _namespaceResolver = new NamespaceResolver();
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            _symbolMapping = new List<ISymbol>();
            var syntaxContext = await SyntaxContext.Create(context.Document, context.Position, context.CancellationToken)
                .ConfigureAwait(false);

            var publicClasses = GetSymbols(syntaxContext);
            var completionItemsToAdd = publicClasses
                .Select(symbol => CreateCompletionItemForSymbol(symbol, syntaxContext))
                .ToList();

            context.AddItems(completionItemsToAdd);
        }

        public override async Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            //Add encodedSymbol to properties
            if (TryGetSymbolMapping(item, out ISymbol symbol))
            {
                string symbolKey = SymbolCompletionItem.EncodeSymbol(symbol);
                item = item.AddProperty(SymbolsProperty, symbolKey);
            }

            var description = await SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken).ConfigureAwait(false);

            // Adding 'unimpoted' text to beginning
            var unimportedTextParts = ImmutableArray<TaggedText>.Empty
                .Add(new TaggedText(TextTags.Text, "(unimported)"))
                .Add(new TaggedText(TextTags.Space, " "))
                .AddRange(description.TaggedParts);

            return description.WithTaggedParts(unimportedTextParts);
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        {
            var change = await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);

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
            var accessabilityTag = typeSymbol.DeclaredAccessibility == Accessibility.Public
                ? CompletionTags.Public
                : CompletionTags.Private;
            var tags = ImmutableArray.Create(CompletionTags.Class, accessabilityTag);

            // In original Roslyn SymbolCompletionProvider SymbolsProperty is set
            // in ProvideCompletionsAsync for all items. However, for huge items quantity
            // encoding has significant performance impact. Instead, we are leaving reference
            // for cached symbol, and encode in GetDescriptionAsync.

            _symbolMapping.Add(typeSymbol);
            string symbolIndexString = (_symbolMapping.Count - 1).ToString();

            //Make those items the least prioritized
            var rules = CompletionItemRules.Create(
                    matchPriority: -1
                );

            var props = ImmutableDictionary<string, string>.Empty
                .Add(ContextPositionProperty, context.Position.ToString())
                .Add(SymbolNameProperty, typeSymbol.Name)
                .Add(SymbolIndexProperty, symbolIndexString);

            // Add namespace to the end so items with same name would be displayed
            var sortText = typeSymbol.Name + " " + typeSymbol.GetNamespace();

            return CompletionItem.Create(
                displayText: CompletionItemHelper.GetDisplayText(typeSymbol, context),
                sortText: sortText,
                properties: props,
                rules: rules,
                tags: tags);
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
            if (context.IsTypeContext)
            {
                var typeSymbols = GetAllTypes(context);
                if (context.IsAttributeContext)
                {
                    typeSymbols = FilterAttributes(typeSymbols);
                }
                return typeSymbols;
            }
            else
            {
                return Enumerable.Empty<ISymbol>();
            }
        }

        private bool TryGetSymbolMapping(CompletionItem item, out ISymbol symbol)
        {
            symbol = null;
            if (item.Properties.TryGetValue(SymbolIndexProperty, out string symbolIndexString))
            {
                int index = int.Parse(symbolIndexString);
                symbol = _symbolMapping[index];
                return true;
            }
            return false;
        }

        private List<INamedTypeSymbol> GetAllTypes(SyntaxContext context)
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

            return foundTypes;
        }

        private bool FilterNamespace(INamespaceSymbol ns)
        {
            bool userCodeOnly = OptionsProvider.Options.UserCodeOnlySuggestions;
            return (!userCodeOnly || ns.Locations.Any(l => l.IsInSource))
                 && ns.CanBeReferencedByName;
        }

        private List<INamedTypeSymbol> FilterAttributes(IEnumerable<INamedTypeSymbol> list)
        {
            return list
                .Where(ts => ts.IsAttribute() && !ts.IsAbstract)
                .ToList();
        }

        private bool FilterType(INamedTypeSymbol type, SyntaxContext syntaxContext)
        {
            return (type.DeclaredAccessibility == Accessibility.Public
                    || (type.DeclaredAccessibility == Accessibility.Internal
                        && type.ContainingAssembly == syntaxContext.SemanticModel.Compilation.Assembly))
                && type.CanBeReferencedByName
                && !syntaxContext.ImportedNamespaces.Contains(type.GetNamespace());
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
