using System;

namespace IntelliSenseExtender.Options
{
	public class VsSettingsOptionsProvider : IOptionsProvider
	{
		internal static Func<OptionsPage> GetOptionsPageFunc;
		internal static Options CachedOptions;

		public static VsSettingsOptionsProvider Current = new VsSettingsOptionsProvider();

		public Options GetOptions()
		{
			if (CachedOptions == null) {
				UpdateCachedOptions();
			}
			return CachedOptions;
		}

		private void UpdateCachedOptions()
		{
			var optionsPage = GetOptionsPageFunc.Invoke();
			CachedOptions = new Options {
				UserCodeOnlySuggestions = optionsPage.UserCodeOnlySuggestions,
				SortCompletionsAfterImported = optionsPage.SortCompletionsAfterImported,
				EnableTypesSuggestions = optionsPage.EnableTypesSuggestions,
				EnableExtensionMethodsSuggestions = optionsPage.EnableExtensionMethodsSuggestions,
				EnableStaticSuggestions = optionsPage.EnableStaticSuggestions,
				StaticSuggestionsOnlyForImportedNamespaces = optionsPage.StaticSuggestionsOnlyForImportedNamespaces,
				StaticSuggestionsAsCodeFixes = optionsPage.StaticSuggestionsAsCodeFixes,
				FilterOutObsoleteSymbols = optionsPage.FilterOutObsoleteSymbols
			};
		}
	}
}
