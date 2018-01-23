using System;
using System.Linq;
using Microsoft.CodeAnalysis.Options;

namespace IntelliSenseExtender.ExposedInternals
{
    public static class GenerationOptions
    {
        public static readonly PerLanguageOption<bool> PlaceSystemNamespaceFirst;

        static GenerationOptions()
        {
            var workspacesAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .First(a => a.GetName().Name == "Microsoft.CodeAnalysis.Workspaces");
            var type = workspacesAssembly.GetType("Microsoft.CodeAnalysis.Editing.GenerationOptions");
            PlaceSystemNamespaceFirst = (PerLanguageOption<bool>)type.GetField("PlaceSystemNamespaceFirst").GetValue(null);
        }
    }
}
