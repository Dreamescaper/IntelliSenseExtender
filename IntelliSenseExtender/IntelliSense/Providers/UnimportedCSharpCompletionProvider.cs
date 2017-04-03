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

namespace IntelliSenseExtender.IntelliSense.Providers
{
    [ExportCompletionProvider("Unimported provider", LanguageNames.CSharp)]
    public class UnimportedCSharpCompletionProvider : CompletionProvider
    {
        private readonly NamespaceResolver _namespaceResolver;
        private Dictionary<string, ISymbol> _symbolMapping;

        public UnimportedCSharpCompletionProvider()
        {
            _namespaceResolver = new NamespaceResolver();
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
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

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            if (TryGetSymbolMapping(item, out ISymbol symbol))
            {
                return CompletionItemHelper.GetUnimportedDescriptionAsync(document, item, symbol, cancellationToken);
            }
            else
            {
                return base.GetDescriptionAsync(document, item, cancellationToken);
            }
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
            var completionItem = CompletionItemHelper.CreateCompletionItem(typeSymbol, context);

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
            if (context.IsTypeContext)
            {
                var typeSymbols = GetAllTypes(context);
                if (context.IsAttributeContext)
                {
                    typeSymbols = FilterAttributes(typeSymbols);
                }
                return typeSymbols;
            }
            else if (context.IsMemberAccessContext)
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

        private List<IMethodSymbol> GetApplicableExtensionMethods(SyntaxContext context)
        {
            var accessedTypeSymbol = context.AccessedTypeSymbol;
            return GetAllTypes(context)
                .Where(type => type.MightContainExtensionMethods)
                .SelectMany(type => type.GetMembers())
                .OfType<IMethodSymbol>()
                .Where(method => method.IsExtensionMethod
                    && method.Parameters.Length > 0
                    && method.Parameters[0].Type.IsAssignableFrom(accessedTypeSymbol))
                .ToList();
        }

        private bool FilterNamespace(INamespaceSymbol ns)
        {
            bool userCodeOnly = OptionsProvider.Options.UserCodeOnlySuggestions;
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
