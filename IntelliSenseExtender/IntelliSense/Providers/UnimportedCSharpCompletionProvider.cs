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
      if (Options.EnableUnimportedSuggestions) {
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

    public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
    {
      string insertText;
      if (!item.Properties.TryGetValue(CompletionItemProperties.InsertText, out insertText)) {
        insertText = item.DisplayText;
      }

      // Add using for required symbol. 
      // Any better place to put this?
      if (TryGetSymbolMapping(item, out ISymbol symbol) && IsCommitContext()) {
        if (symbol.IsStaticImportable()) {
          if (Options.StaticSuggestionsAsCodeFixes) {
            var ns = symbol.GetNamespace();

            var fullname = symbol.ToDisplayString();
            insertText = fullname //change insertText so that it includes Class name and import namespace if necessary
              .Replace(ns + ".", "") //remove namespace from the fully qualified name
              ;
            //remove (list of params)
            var index_of_parenthesis = insertText.IndexOf('(');
            if (index_of_parenthesis > -1) insertText = insertText.Remove(index_of_parenthesis);

            //we may still need to import namespace for the class, so we check for that here
            var syntaxTree = await document.GetSyntaxTreeAsync().ConfigureAwait(false);
            var importedNamespaces = syntaxTree.GetImportedNamespaces();
            if (!importedNamespaces.Contains(ns)) _namespaceResolver.AddNamespaceOrStatic(ns, true);

          }
          else _namespaceResolver.AddNamespaceOrStatic(symbol.ContainingType.ToDisplayString(), false);
        }
        else _namespaceResolver.AddNamespaceOrStatic(symbol.GetNamespace(), true);
      }

      return CompletionChange.Create(new TextChange(item.Span, insertText));
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
      if (syntaxRoot is CompilationUnitSyntax compilationUnitSyntax) {
        var usingNames = nameSpaces
            .Select(ns => SyntaxFactory.ParseName(ns))
            .Select(nsName => SyntaxFactory.UsingDirective(nsName).NormalizeWhitespace())
            .ToArray();
        compilationUnitSyntax = compilationUnitSyntax.AddUsings(usingNames);
        return document.WithSyntaxRoot(compilationUnitSyntax);
      }
      else {
        return document;
      }
    }

    private IEnumerable<ISymbol> GetSymbols(SyntaxContext context)
    {

      if (context.IsTypeContext) {
        var typeSymbols = Enumerable.Empty<INamedTypeSymbol>().ToList();
        if (Options.EnableTypesSuggestions) {
          typeSymbols = GetAllTypes(context);
          if (context.IsAttributeContext) {
            typeSymbols = FilterAttributes(typeSymbols);
          }
        }
        if (Options.EnableStaticSuggestions && !context.IsAttributeContext) {
          var staticSymbols = GetAllStaticSymbols(context);
          staticSymbols.AddRange(typeSymbols);
          return staticSymbols;
        }
        return typeSymbols;
      }

      if (Options.EnableExtensionMethodsSuggestions
          && context.IsMemberAccessContext
          && context.AccessedSymbol?.Kind != SymbolKind.NamedType
          && context.AccessedSymbolType != null) {
        return GetApplicableExtensionMethods(context);
      }

      return Enumerable.Empty<ISymbol>();
    }

    private bool TryGetSymbolMapping(CompletionItem item, out ISymbol symbol)
    {
      symbol = null;
      if (item.Properties.TryGetValue(CompletionItemProperties.FullSymbolName,
          out string fullSymbolName)) {
        return _symbolMapping.TryGetValue(fullSymbolName, out symbol);
      }
      return false;
    }

    private List<INamedTypeSymbol> GetAllTypes(SyntaxContext context, bool forStatics = false)
    {
      const int typesCapacity = 100000;

      var foundTypes = new List<INamedTypeSymbol>(typesCapacity);

      var opt = Options;

      var namespacesToTraverse = new[] { context.SemanticModel.Compilation.GlobalNamespace };
      while (namespacesToTraverse.Length > 0) {
        var members = namespacesToTraverse.SelectMany(ns => ns.GetMembers()).ToArray();
        var typeSymbols = members
            .OfType<INamedTypeSymbol>()
            .Where(symbol => FilterType(symbol, context, forStatics, opt));
        foundTypes.AddRange(typeSymbols);
        namespacesToTraverse = members
            .OfType<INamespaceSymbol>()
            .Where(FilterNamespace)
            .ToArray();
      }

      return FilterOutObsoleteSymbolsIfNeeded(foundTypes);
    }

    private List<ISymbol> GetAllStaticSymbols(SyntaxContext context)
    {
      var foundMethods = GetAllTypes(context, true)
          .Where(type => type.IsStatic || type.IsValueType)
          .SelectMany(type => type.GetMembers())
          .Where(m => m != null && m.IsStaticImportable())
          .ToList();

      return FilterOutObsoleteSymbolsIfNeeded(foundMethods);
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

    private bool FilterType(INamedTypeSymbol type, SyntaxContext syntaxContext, bool forStatics, Options.Options options)
    {
      return (type.DeclaredAccessibility == Accessibility.Public
              || (type.DeclaredAccessibility == Accessibility.Internal
                  && type.ContainingAssembly == syntaxContext.SemanticModel.Compilation.Assembly))
          && type.CanBeReferencedByName
          &&
          ((!forStatics && !syntaxContext.ImportedNamespaces.Contains(type.GetNamespace())) ||
          (forStatics && !syntaxContext.ImportedStatics.Contains(type.ToDisplayString())
          && (!options.StaticSuggestionsOnlyForImportedNamespaces || (syntaxContext.ImportedNamespaces.Contains(type.GetNamespace()))) //limit to types from imported namespaces
          ))

          ;
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
