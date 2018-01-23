using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using IntelliSenseExtender.Extensions;
using IntelliSenseExtender.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;

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
            return CompletionItemHelper.CreateCompletionItem(typeSymbol, context, sorting);
        }

        private IEnumerable<ISymbol> GetSymbols(SyntaxContext context)
        {
            if (Options.EnableTypesSuggestions && context.IsTypeContext)
            {
                var typeSymbols = GetAllTypes(context, context.CancellationToken);
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
            var foundExtensionSymbols = GetAllTypes(context, context.CancellationToken)
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
