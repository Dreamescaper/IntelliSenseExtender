using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.ExposedInternals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace IntelliSenseExtender.Editor
{
    public class NamespaceResolver
    {
        public async Task<Document> AddNamespaceImportAsync(string nsName, Document document, int position, CancellationToken cancellationToken)
        {
            var documentOptionsTask = document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (model == null)
                return document;

            var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            var currentNode = root.SyntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken).Parent;

            var documentOptions = await documentOptionsTask;
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
                var import = SyntaxGenerator.GetGenerator(document)
                    .NamespaceImportDeclaration(nsName);

                var services = document.Project.LanguageServices;
                var finalRoot = services.AddImports(
                    model.Compilation, root, currentNode, new[] { import }, placeSystemNamespaceFirst);

                var newDocument = document.WithSyntaxRoot(finalRoot);
                newDocument = await Formatter.FormatAsync(newDocument, SyntaxAnnotation.ElasticAnnotation).ConfigureAwait(false);

                return newDocument;
            }
            else
            {
                return document;
            }
        }

        private string ReduceNamespaceName(string nsToImport, string containingNs)
        {
            const char dot = '.';

            var indexFromCopy = 0;
            var maxIndex = Math.Min(nsToImport.Length, containingNs.Length) - 1;

            var index = 0;
            for (; index < maxIndex; index++)
            {
                if (nsToImport[index] != containingNs[index])
                    break;

                if (nsToImport[index] == dot)
                    indexFromCopy = index + 1;
            }

            var isLastComparisionSuccessful = index == maxIndex && nsToImport[index] == containingNs[index];
            var nextSymbolIsDot = ++index < nsToImport.Length - 1 && nsToImport[index] == dot;

            if (isLastComparisionSuccessful && nextSymbolIsDot)
                indexFromCopy = index + 1;

            return nsToImport.Substring(indexFromCopy);
        }
    }
}
