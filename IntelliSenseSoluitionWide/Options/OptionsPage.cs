using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace IntelliSenseExtender.Options
{
    [Guid("4BAE5E7B-F8F9-454D-A514-67444B0FDA00")]
    [ComVisible(true)]
    public class OptionsPage : DialogPage
    {
        [Category("Completions")]
        [DefaultValue(false)]
        [DisplayName("User Code Only Suggestions")]
        [Description("Enable, if you want to limit suggestion to user code only. " +
            "If disabled search will be perfomed in user code and all referenced assemblies.")]
        public bool UserCodeOnlySuggestions { get; set; }
    }
}
