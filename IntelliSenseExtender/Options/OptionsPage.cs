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
        [DefaultValue(true)]
        [DisplayName("Suggest Unimported Types")]
        [Description("Show IntelliSense suggestions for unimported types. (Not needed for 16.1+)")]
        public bool SuggestUnimportedTypes { get; set; }

        [Category("Unimported Symbols")]
        [DefaultValue(true)]
        [DisplayName("Suggest Unimported Extension Methods")]
        [Description("Show IntelliSense suggestions for unimported extension methods.")]
        public bool SuggestUnimportedExtensionMethods { get; set; }

        [Category("Unimported Symbols")]
        [DefaultValue(true)]
        [DisplayName("Sort Added Suggestions Last")]
        [Description("If enabled, unimported completions are listed after imported ones. " +
            "Otherwise all items are sorted alphabetically.")]
        public bool SortCompletionsAfterImported { get; set; }

        [Category("Unimported Symbols")]
        [DefaultValue(true)]
        [DisplayName("Filter Out Obsolete symbols")]
        [Description("Do not show methods or types marked with Obsolete attribute.")]
        public bool FilterOutObsoleteSymbols { get; set; }

        [Category("Unimported Symbols")]
        [DefaultValue(false)]
        [DisplayName("Suggest Nested Types")]
        [Description("Suggest nested types in arbitrary context.")]
        public bool SuggestNestedTypes { get; set; }

        [Category("Object Creation")]
        [DefaultValue(true)]
        [DisplayName("Suggest types on object creation")]
        [Description("If type is known, suggest it or its ancestor types after new keyword. " +
            "Might lead to duplications in IntelliSense.")]
        public bool SuggestTypesOnObjectCreation { get; set; }

        [Category("Object Creation")]
        [DefaultValue(true)]
        [DisplayName("Add parenthesis for 'new' suggestions")]
        [Description("When we create new object, show 'new' completions with parenthesis (including invocation)")]
        public bool AddParethesisForNewSuggestions { get; set; }

        [Category("Object Creation")]
        [DefaultValue(true)]
        [DisplayName("Suggest static factory methods on object creation")]
        [Description("On object creation suggest static factory methods or static properties, " +
            "if there are any in target type.")]
        public bool SuggestFactoryMethodsOnObjectCreation { get; set; }

        [Category("Object Creation")]
        [DefaultValue(true)]
        [DisplayName("Suggest suitable variables as method parameters first. " +
            "Might lead to duplications in IntelliSense.")]
        public bool SuggestLocalVariablesFirst { get; set; }

        [Category("Object Creation")]
        [DefaultValue(true)]
        [DisplayName("Invoke IntelliSense automatically when applicable")]
        [Description("(e.g. during assignment or providing arguments)")]
        public bool InvokeIntelliSenseAutomatically { get; set; }

        public OptionsPage()
        {
            SuggestUnimportedTypes = true;
            SuggestUnimportedExtensionMethods = true;
            SortCompletionsAfterImported = true;
            FilterOutObsoleteSymbols = true;
            SuggestNestedTypes = false;
            SuggestTypesOnObjectCreation = true;
            AddParethesisForNewSuggestions = true;
            SuggestFactoryMethodsOnObjectCreation = true;
            SuggestLocalVariablesFirst = true;
            InvokeIntelliSenseAutomatically = true;
        }
    }
}