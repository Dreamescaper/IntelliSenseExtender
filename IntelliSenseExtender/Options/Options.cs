using System;

namespace IntelliSenseExtender.Options
{
    public class Options
    {
        public bool SuggestUnimportedTypes { get; set; }
        public bool SuggestUnimportedExtensionMethods { get; set; }
        public bool FilterOutObsoleteSymbols { get; set; }
        public bool SuggestTypesOnObjectCreation { get; set; }
        public bool AddParethesisForNewSuggestions { get; set; }
        public bool SuggestFactoryMethodsOnObjectCreation { get; set; }
        public bool SuggestLocalVariablesFirst { get; set; }
        public bool InvokeIntelliSenseAutomatically { get; set; }
        public bool SuggestNestedTypes { get; set; }

        public static Version VsVersion { get; set; }
    }
}
