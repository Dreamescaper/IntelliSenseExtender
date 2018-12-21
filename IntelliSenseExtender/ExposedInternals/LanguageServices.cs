using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace IntelliSenseExtender.ExposedInternals
{
    public static class LanguageServices
    {
        private static readonly Type _addImportServiceType;
        private static readonly MethodInfo _addImportsMethod;
        private static readonly MethodInfo _getServiceMethod;

        static LanguageServices()
        {
            var workspacesAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .First(a => a.GetName().Name == "Microsoft.CodeAnalysis.Workspaces");
            _addImportServiceType = workspacesAssembly.GetType("Microsoft.CodeAnalysis.AddImports.IAddImportsService");
            _addImportsMethod = _addImportServiceType.GetMethod("AddImports");
            _getServiceMethod = typeof(HostLanguageServices)
                .GetMethod(nameof(HostLanguageServices.GetService), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }

        public static SyntaxNode AddImports(this HostLanguageServices hostServices,
            Compilation compilation, SyntaxNode root, SyntaxNode contextLocation,
            IEnumerable<SyntaxNode> newImports, bool placeSystemNamespaceFirst)
        {
            var addImportService = GetService(hostServices, _addImportServiceType);
            return (SyntaxNode)_addImportsMethod.Invoke(addImportService, new object[] { compilation, root, contextLocation, newImports, placeSystemNamespaceFirst });
        }

        private static object GetService(HostLanguageServices hostServices, Type serviceType)
        {
            return _getServiceMethod.MakeGenericMethod(serviceType)
                .Invoke(hostServices, null);
        }
    }
}
