using System;

namespace IntelliSenseExtender.Options
{
    public class OptionsProvider
    {
        internal static Func<OptionsPage> GetOptionsPageFunc;
        internal static Options CachedOptions;

        public static Options Options
        {
            get
            {
                if (CachedOptions == null)
                {
                    UpdateCachedOptions();
                }
                return CachedOptions;
            }
        }

        public static void UpdateCachedOptions()
        {
            var optionsPage = GetOptionsPageFunc.Invoke();
            CachedOptions = new Options
            {
                UserCodeOnlySuggestions = optionsPage.UserCodeOnlySuggestions,
                SortCompletionsAfterImported = optionsPage.SortCompletionsAfterImported,
                EnableTypesSuggestions = optionsPage.EnableTypesSuggestions,
                EnableExtensionMethodsSuggestions = optionsPage.EnableExtensionMethodsSuggestions
            };
        }
    }
}
