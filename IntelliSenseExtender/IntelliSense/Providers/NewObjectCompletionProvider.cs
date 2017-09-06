using IntelliSenseExtender.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading.Tasks;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    [ExportCompletionProvider("Object creation provider", LanguageNames.CSharp)]
    public class NewObjectCompletionProvider : AbstractCompletionProvider
    {
        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var syntaxContext = await SyntaxContext.Create(context.Document, context.Position, context.CancellationToken);

            ObjectCreationExpressionSyntax objCreation;
            if (syntaxContext.SyntaxTree.IsObjectCreationContext(context.Position, out objCreation, context.CancellationToken))
            {
                ITypeSymbol typeSymbol;
                if (TryGetTypeSymbol(objCreation, syntaxContext.SemanticModel, out typeSymbol))
                {
                    var symbols = GetAllTypes(syntaxContext)
                        .Select(type => syntaxContext.SemanticModel.Compilation.GetAssignableSymbol(type, typeSymbol))
                        .Where(type => type != null)
                        .ToList();
                    symbols.ForEach(symbol => context.AddItem(CreateCompletionItem(symbol, syntaxContext, true)));
                }
            }
        }

        protected override bool FilterType(INamedTypeSymbol type, SyntaxContext syntaxContext)
        {
            return
                (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct) &&
                !type.IsAbstract &&
                type.InstanceConstructors.Any(con => con.DeclaredAccessibility == Accessibility.Public);
        }

        private CompletionItem CreateCompletionItem(ITypeSymbol typeSymbol, SyntaxContext syntaxContext, bool preselect)
        {
            var i = CompletionItemHelper.CreateCompletionItem(typeSymbol, syntaxContext, false, preselect ? MatchPriority.Preselect : MatchPriority.Default);
            return i.WithSortText("!" + i.SortText).WithDisplayText(i.DisplayText + "bla");
        }

        private bool TryGetTypeSymbol(ObjectCreationExpressionSyntax objectCreationSyntax, SemanticModel semanticModel, out ITypeSymbol typeSymbol)
        {
            typeSymbol = null;

            var parentSyntax = objectCreationSyntax.Parent;

            if (parentSyntax.Parent.Parent is VariableDeclarationSyntax varDeclarationSyntax
                && !varDeclarationSyntax.Type.IsVar)
            {
                var typeInfo = semanticModel.GetTypeInfo(varDeclarationSyntax.Type);
                typeSymbol = typeInfo.Type;
            }
            else if (parentSyntax is AssignmentExpressionSyntax assigmentExpressionSyntax)
            {
                var typeInfo = semanticModel.GetTypeInfo(assigmentExpressionSyntax.Left);
                typeSymbol = typeInfo.Type;
            }
            else if (parentSyntax is ArgumentSyntax argumentSyntax)
            {
                typeSymbol = semanticModel.GetParameterTypeSymbol(argumentSyntax);
            }

            return typeSymbol != null;
        }
    }
}
