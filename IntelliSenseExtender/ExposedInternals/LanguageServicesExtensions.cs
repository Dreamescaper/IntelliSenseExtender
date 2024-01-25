using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;

#nullable disable

namespace IntelliSenseExtender.ExposedInternals
{
    public static class LanguageServicesExtensions
    {
        private static readonly Type _addImportServiceType;
        private static readonly MethodInfo _addImportPlacementOptionsFromDocumentMethod;
        private static readonly MethodInfo _addImportsMethod;
        private static readonly MethodInfo _getServiceMethod;

        static LanguageServicesExtensions()
        {
            var workspacesAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .First(a => a.GetName().Name == "Microsoft.CodeAnalysis.Workspaces");

            // Before 17.2 - AddImports, after - AddImport.
            _addImportServiceType = workspacesAssembly.GetType("Microsoft.CodeAnalysis.AddImport.IAddImportsService");

            var addImportPlacementOptionsType = workspacesAssembly.GetType("Microsoft.CodeAnalysis.AddImport.AddImportPlacementOptions");
            var addImportPlacementOptionsProviderType = workspacesAssembly.GetType("Microsoft.CodeAnalysis.AddImport.AddImportPlacementOptionsProviders");
            _addImportPlacementOptionsFromDocumentMethod = addImportPlacementOptionsProviderType.GetMethod("GetAddImportPlacementOptionsAsync",
                [typeof(Document), addImportPlacementOptionsType, typeof(CancellationToken)]); ;

            _addImportsMethod = _addImportServiceType.GetMethod("AddImports");
            _getServiceMethod = typeof(LanguageServices)
                .GetMethod(nameof(LanguageServices.GetService), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        public static async ValueTask<SyntaxNode> AddImportsAsync(this LanguageServices hostServices, Document document,
            Compilation compilation, SyntaxNode root, SyntaxNode contextLocation,
            IEnumerable<SyntaxNode> newImports, SyntaxGenerator syntaxGenerator,
            CancellationToken cancellationToken)
        {
            var addImportService = GetService(hostServices, _addImportServiceType);

            var placementOptions = await GetPlacementOptionsAsync(document, cancellationToken);
            var arguments = new object[] { compilation, root, contextLocation, newImports, syntaxGenerator, placementOptions, cancellationToken };

            return (SyntaxNode)_addImportsMethod.Invoke(addImportService, arguments);
        }

        private static async ValueTask<object> GetPlacementOptionsAsync(Document document, CancellationToken token)
        {
            var task = _addImportPlacementOptionsFromDocumentMethod.Invoke(null, [document, null, token]);
            return await AwaitValueTaskAsync(task);
        }

        private static object GetService(LanguageServices hostServices, Type serviceType)
        {
            return _getServiceMethod.MakeGenericMethod(serviceType).Invoke(hostServices, null);
        }

        private static async ValueTask<object> AwaitValueTaskAsync(object valueTask)
        {
            var type = valueTask.GetType();
            var isCompleted = (bool)type.GetProperty(nameof(ValueTask.IsCompleted)).GetValue(valueTask);
            if (!isCompleted)
            {
                var task = (Task)type.GetMethod(nameof(ValueTask.AsTask)).Invoke(valueTask, []);
                await task;
            }

            return type.GetProperty(nameof(ValueTask<object>.Result)).GetValue(valueTask);
        }
    }
}
