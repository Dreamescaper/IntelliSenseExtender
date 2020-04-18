using System;
using System.Collections;
using System.Linq;
using Microsoft.CodeAnalysis.Options;

#nullable disable
#pragma warning disable REFL009 // The referenced member is not known to exist.

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
            var option = type?.GetField("PlaceSystemNamespaceFirst")?.GetValue(null);

            if (option is PerLanguageOption<bool> oldOption)
            {
                PlaceSystemNamespaceFirst = oldOption;
            }
            else
            {
                // VS 16.6P3
                // option is PerLanguageOption2<bool>

                var optionType = option.GetType();
                var storageLocations = optionType.GetProperty("StorageLocations").GetValue(option) as IEnumerable;

                PlaceSystemNamespaceFirst = new PerLanguageOption<bool>(
                    nameof(GenerationOptions),
                    nameof(PlaceSystemNamespaceFirst),
                    defaultValue: true,
                    storageLocations: storageLocations.OfType<OptionStorageLocation>().ToArray());
            }
        }
    }
}
