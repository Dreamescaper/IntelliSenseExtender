using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.ExposedInternals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Package = Microsoft.VisualStudio.Shell.Package;

namespace IntelliSenseExtender.Editor
{
    public class NamespaceResolver
    {
        private VisualStudioWorkspace GetVisualStudioWorkspace()
        {
            var componentModel = (IComponentModel)Package.GetGlobalService(typeof(SComponentModel));
            return componentModel.GetService<VisualStudioWorkspace>();
        }

        /// <summary>
        /// Add namespace to current document
        /// </summary>
        public async Task AddNamespaceAndApplyAsync(string nsName, Document document, CancellationToken cancellationToken)
        {
            var workspace = GetVisualStudioWorkspace();

            // Provided document might be outdated at this point. Find recent one.
            var recentDocument = workspace.CurrentSolution.GetDocument(document.Id);
            var newDocument = await AddNamespaceImportAsync(nsName, recentDocument, cancellationToken).ConfigureAwait(false);

            bool addedSuccessfully = workspace.TryApplyChanges(newDocument.Project.Solution);

            Debug.Assert(addedSuccessfully, "Failed to add import!");
        }

        public async Task<Document> AddNamespaceImportAsync(string nsName, Document document, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var documentOptions = await document.GetOptionsAsync().ConfigureAwait(false);

            bool placeSystemNamespaceFirst = documentOptions.GetOption(GenerationOptions.PlaceSystemNamespaceFirst);

            var import = SyntaxGenerator.GetGenerator(document)
                .NamespaceImportDeclaration(nsName)
                .NormalizeWhitespace()
                .WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n"));

            var services = document.Project.LanguageServices;
            var finalRoot = services.AddImports(
                model.Compilation, root, root, new[] { import }, placeSystemNamespaceFirst);

            return document.WithSyntaxRoot(finalRoot);
        }
    }
}
