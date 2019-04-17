namespace IntelliSenseExtender.Options
{
    public class VsSettingsOptionsProvider : IOptionsProvider
    {
        public static VsSettingsOptionsProvider Current { get; set; } = new VsSettingsOptionsProvider();

        public Options GetOptions()
        {
            var optionsPage = IntelliSenseExtenderPackage.OptionsPage;

            Options.VsVersion = IntelliSenseExtenderPackage.VsVersion;

            return optionsPage == null
                ? null
                : new Options
                {
                    SuggestUnimportedTypes = optionsPage.SuggestUnimportedTypes,
                    SuggestUnimportedExtensionMethods = optionsPage.SuggestUnimportedExtensionMethods,
                    SortCompletionsAfterImported = optionsPage.SortCompletionsAfterImported,
                    FilterOutObsoleteSymbols = optionsPage.FilterOutObsoleteSymbols,
                    SuggestNestedTypes = optionsPage.SuggestNestedTypes,
                    SuggestTypesOnObjectCreation = optionsPage.SuggestTypesOnObjectCreation,
                    AddParethesisForNewSuggestions = optionsPage.AddParethesisForNewSuggestions,
                    SuggestFactoryMethodsOnObjectCreation = optionsPage.SuggestFactoryMethodsOnObjectCreation,
                    SuggestLocalVariablesFirst = optionsPage.SuggestLocalVariablesFirst,
                    InvokeIntelliSenseAutomatically = optionsPage.InvokeIntelliSenseAutomatically
                };
        }
    }
}