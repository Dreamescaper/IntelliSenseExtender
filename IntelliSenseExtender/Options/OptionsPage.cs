using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace IntelliSenseExtender.Options
{
    [Guid("4BAE5E7B-F8F9-454D-A514-67444B0FDA00")]
    [ComVisible(true)]
    public class OptionsPage : DialogPage
    {
        [Category("Unimported Symbols")]
        [DefaultValue(false)]
        [DisplayName("User Code Only Suggestions")]
        [Description("Enable, if you want to limit suggestion to user code only. " +
            "If disabled search will be performed in user code and all referenced assemblies.")]
        public bool UserCodeOnlySuggestions { get; set; }

        [Category("Unimported Symbols")]
        [DefaultValue(true)]
        [DisplayName("Sort Added Suggestions Last")]
        [Description("If enabled, unimported completions are listed after imported ones. " +
            "Otherwise all items are sorted alphabetically.")]
        public bool SortCompletionsAfterImported { get; set; }

        [Category("Unimported Symbols")]
        [DefaultValue(true)]
        [DisplayName("Enable Types Suggestions")]
        [Description("Show IntelliSense suggestions for unimported types.")]
        public bool EnableTypesSuggestions { get; set; }

        [Category("Unimported Symbols")]
        [DefaultValue(true)]
        [DisplayName("Enable Extension Methods Suggestions")]
        [Description("Show IntelliSense suggestions for unimported extension methods.")]
        public bool EnableExtensionMethodsSuggestions { get; set; }

        [Category("Unimported Symbols")]
        [DefaultValue(true)]
        [DisplayName("Filter Out Obsolete symbols")]
        [Description("Do not show methods or types marked with Obsolete attribute.")]
        public bool FilterOutObsoleteSymbols { get; set; }

        [Category("Object Creation")]
        [DefaultValue(true)]
        [DisplayName("Suggest types on object creation")]
        [Description("If type is known, suggest it or its ancestor types after new keyword. " +
            "Might lead to duplications in IntelliSense.")]
        public bool SuggestOnObjectCreation { get; set; }

        public OptionsPage()
        {
            UserCodeOnlySuggestions = false;
            SortCompletionsAfterImported = true;
            EnableTypesSuggestions = true;
            EnableExtensionMethodsSuggestions = true;
            FilterOutObsoleteSymbols = true;
            SuggestOnObjectCreation = true;
        }

        public override void SaveSettingsToStorage()
        {
            base.SaveSettingsToStorage();
            VsSettingsOptionsProvider.CachedOptions = null;
        }
    }
}
