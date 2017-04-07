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
            "If disabled search will be perfomed in user code and all referenced assemblies.")]
        public bool UserCodeOnlySuggestions { get; set; }

        [Category("Unimported Symbols")]
        [DefaultValue(false)]
        [DisplayName("Sort Completions Last")]
        [Description("If enabled, unimported completions are listed after imported ones. " +
            "Otherwise all items are sorted alphabetically.")]
        public bool SortCompletionsAfterImported { get; set; }

        public override void SaveSettingsToStorage()
        {
            base.SaveSettingsToStorage();
            OptionsProvider.CachedOptions = null;
        }
    }
}
