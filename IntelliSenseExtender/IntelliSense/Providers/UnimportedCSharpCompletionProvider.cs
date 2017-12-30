using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    [ExportCompletionProvider("Unimported provider", LanguageNames.CSharp)]
    public class UnimportedCSharpCompletionProvider : AbstractCompletionProvider
    {
        public UnimportedCSharpCompletionProvider() : base()
        {
        }

        public UnimportedCSharpCompletionProvider(IOptionsProvider optionsProvider) : base(optionsProvider)
        {
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            if (Options.EnableUnimportedSuggestions)
            {
                var syntaxContext = await SyntaxContext.Create(context.Document, context.Position, context.CancellationToken)
                    .ConfigureAwait(false);
                var symbols = GetSymbols(syntaxContext);

                var completionItemsToAdd = symbols
                    .Select(symbol => CreateCompletionItemForSymbol(symbol, syntaxContext))
                    .ToList();

                context.AddItems(completionItemsToAdd);
            }
        }

        private CompletionItem CreateCompletionItemForSymbol(ISymbol typeSymbol, SyntaxContext context)
        {
            int sorting = Options.SortCompletionsAfterImported ? Sorting.Last : Sorting.Default;
            var completionItem = CompletionItemHelper.CreateCompletionItem(typeSymbol, context, sorting);

            var fullSymbolName = completionItem.Properties[CompletionItemProperties.FullSymbolName];
            GetSymbolMapping(context.Document)[fullSymbolName] = typeSymbol;

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

        private IEnumerable<IMethodSymbol> GetApplicableExtensionMethods(SyntaxContext context)
        {
            var accessedTypeSymbol = context.AccessedSymbolType;
            var foundExtensionSymbols = GetAllTypes(context)
                .Where(type => type.MightContainExtensionMethods)
                .SelectMany(type => type.GetMembers())
                .OfType<IMethodSymbol>()
                .Select(m => m.ReduceExtensionMethod(accessedTypeSymbol))
                .Where(m => m != null);

            return FilterOutObsoleteSymbolsIfNeeded(foundExtensionSymbols);
        }

        protected override bool FilterType(INamedTypeSymbol type, SyntaxContext syntaxContext)
        {
            return base.FilterType(type, syntaxContext)
                && !syntaxContext.ImportedNamespaces.Contains(type.GetNamespace());
        }

        private List<INamedTypeSymbol> FilterAttributes(IEnumerable<INamedTypeSymbol> list)
        {
            return list
                .Where(ts => ts.IsAttribute() && !ts.IsAbstract)
                .ToList();
        }
    }
}
