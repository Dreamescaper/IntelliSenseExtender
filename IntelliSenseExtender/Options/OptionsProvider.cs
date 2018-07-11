using System;

namespace IntelliSenseExtender.Options
{
    public class VsSettingsOptionsProvider : IOptionsProvider
    {
        internal static Options CachedOptions { get; set; }
        internal static Func<OptionsPage> GetOptionsPageFunc { get; set; }

        public static VsSettingsOptionsProvider Current { get; set; } = new VsSettingsOptionsProvider();

        public Options GetOptions()
        {
            if (CachedOptions == null)
            {
                UpdateCachedOptions();
            }
            return CachedOptions;
        }

        private void UpdateCachedOptions()
        {
            var optionsPage = GetOptionsPageFunc();
            CachedOptions = new Options
            {
                SortCompletionsAfterImported = optionsPage.SortCompletionsAfterImported,
                FilterOutObsoleteSymbols = optionsPage.FilterOutObsoleteSymbols,
                SuggestNestedTypes = optionsPage.SuggestNestedTypes,
                SuggestTypesOnObjectCreation = optionsPage.SuggestTypesOnObjectCreation,
                AddParethesisForNewSuggestions = optionsPage.AddParethesisForNewSuggestions,
                SuggestFactoryMethodsOnObjectCreation = optionsPage.SuggestFactoryMethodsOnObjectCreation,
                SuggestLocalVariablesFirst = optionsPage.SuggestLocalVariablesFirst
            };
        }
    }
}
