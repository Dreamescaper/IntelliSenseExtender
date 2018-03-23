using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.ExposedInternals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
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

            var existingUsingContext = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .FirstOrDefault() ?? root;

            var parentNamespace = existingUsingContext.Ancestors()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault()
                ?.Name.GetText().ToString().Trim();

            //If we're adding import inside namespace, added namespace should be reduced relatively to parent namespace
            if (parentNamespace != null)
            {
                nsName = ReduceNamespaceName(nsName, parentNamespace);
            }

            if (!string.IsNullOrWhiteSpace(nsName))
            {
                var import = SyntaxGenerator.GetGenerator(document).NamespaceImportDeclaration(nsName);

                var services = document.Project.LanguageServices;
                var finalRoot = services.AddImports(
                    model.Compilation, root, existingUsingContext, new[] { import }, placeSystemNamespaceFirst);

                var newDocument = document.WithSyntaxRoot(finalRoot);
                newDocument = await Formatter.FormatAsync(newDocument, SyntaxAnnotation.ElasticAnnotation).ConfigureAwait(false);

                return newDocument;
            }
            else
            {
                return document;
            }
        }

        // Has to be better way
        private string ReduceNamespaceName(string nsToImport, string containingNs)
        {
            IEnumerable<string> GetParentNamespaces(string ns)
            {
                yield return ns;

                while (ns.Contains('.'))
                {
                    ns = ns.Substring(0, ns.LastIndexOf('.'));
                    yield return ns;
                }
            }

            if (nsToImport == containingNs)
            {
                return string.Empty;
            }

            foreach (var ns in GetParentNamespaces(containingNs))
            {
                if (nsToImport.StartsWith(ns + "."))
                {
                    return nsToImport.Substring(Math.Min(ns.Length + 1, nsToImport.Length));
                }
            }
            return nsToImport;
        }
    }
}
