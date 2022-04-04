using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

#nullable disable

namespace IntelliSenseExtender.ExposedInternals
{
    public static class LanguageServices
    {
        private static readonly Type _addImportServiceType;
        private static readonly MethodInfo _addImportPlacementOptionsFromDocumentMethod;
        private static readonly MethodInfo _addImportsMethod;
        private static readonly MethodInfo _getServiceMethod;

        static LanguageServices()
        {
            var workspacesAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .First(a => a.GetName().Name == "Microsoft.CodeAnalysis.Workspaces");

            // Before 17.2 - AddImports, after - AddImport.
            _addImportServiceType = workspacesAssembly.GetType("Microsoft.CodeAnalysis.AddImport.IAddImportsService")
                ?? workspacesAssembly.GetType("Microsoft.CodeAnalysis.AddImports.IAddImportsService");

            var addImportPlacementOptionsType = workspacesAssembly.GetType("Microsoft.CodeAnalysis.AddImport.AddImportPlacementOptions");
            _addImportPlacementOptionsFromDocumentMethod = addImportPlacementOptionsType?.GetMethod("FromDocument");

            _addImportsMethod = _addImportServiceType.GetMethod("AddImports");
            _getServiceMethod = typeof(HostLanguageServices)
                .GetMethod(nameof(HostLanguageServices.GetService), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        public static SyntaxNode AddImports(this HostLanguageServices hostServices, Document document,
            Compilation compilation, SyntaxNode root, SyntaxNode contextLocation,
            IEnumerable<SyntaxNode> newImports, SyntaxGenerator syntaxGenerator,
            OptionSet optionSet, CancellationToken cancellationToken)
        {
            var addImportService = GetService(hostServices, _addImportServiceType);

            object[] arguments;
            if (_addImportsMethod.GetParameters().Length == 8)
            {
                // Pre 17.2
                var allowInHiddenRegions = false;
                arguments = new object[] { compilation, root, contextLocation, newImports, syntaxGenerator, optionSet, allowInHiddenRegions, cancellationToken };
            }
            else
            {
                // Post 17.2
                var placementOptions = _addImportPlacementOptionsFromDocumentMethod?.Invoke(null, new object[] { document, optionSet });
                arguments = new object[] { compilation, root, contextLocation, newImports, syntaxGenerator, placementOptions, cancellationToken };
            }

            return (SyntaxNode)_addImportsMethod.Invoke(addImportService, arguments);
        }

        private static object GetService(HostLanguageServices hostServices, Type serviceType)
        {
            return _getServiceMethod.MakeGenericMethod(serviceType)
                .Invoke(hostServices, null);
        }
    }
}
