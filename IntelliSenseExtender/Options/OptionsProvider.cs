namespace IntelliSenseExtender.Options
{
    public class VsSettingsOptionsProvider : IOptionsProvider
    {
        public static VsSettingsOptionsProvider Current { get; set; } = new VsSettingsOptionsProvider();

        public Options? GetOptions()
        {
            var optionsPage = IntelliSenseExtenderPackage.OptionsPage;

            return optionsPage == null
                ? null
                : new Options
                {
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