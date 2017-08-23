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
		[DisplayName("Enable Static Suggestions")]
		[Description("Show IntelliSense suggestions for unimported static members (methods, fields, properties) and constants.")]
		public bool EnableStaticSuggestions { get; set; }

		[Category("Unimported Symbols")]
		[DefaultValue(true)]
		[DisplayName("Filter Out Obsolete symbols")]
		[Description("Do not show methods or types marked with Obsolete attribute.")]
		public bool FilterOutObsoleteSymbols { get; set; }

		[Category("Static Symbols")]
		[DefaultValue(true)]
		[DisplayName("For Imported Namespaces Only")]
		[Description("Show IntelliSense suggestions for unimported static members and constants only if they are declared in types that are already imported. This, in effect considerably, reduces the number of suggested static imports.")]
		public bool StaticSuggestionsOnlyForImportedNamespaces { get; set; }

		public OptionsPage()
		{
			UserCodeOnlySuggestions = false;
			SortCompletionsAfterImported = true;
			EnableTypesSuggestions = true;
			EnableExtensionMethodsSuggestions = true;
			EnableStaticSuggestions = true;
			StaticSuggestionsOnlyForImportedNamespaces = true;
			FilterOutObsoleteSymbols = true;
		}

		public override void SaveSettingsToStorage()
		{
			base.SaveSettingsToStorage();
			VsSettingsOptionsProvider.CachedOptions = null;
		}
	}
}
