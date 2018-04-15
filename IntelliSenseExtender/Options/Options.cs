namespace IntelliSenseExtender.Options
{
    public class Options
    {
        public bool SortCompletionsAfterImported { get; set; }
        public bool EnableTypesSuggestions { get; set; }
        public bool EnableExtensionMethodsSuggestions { get; set; }
        public bool FilterOutObsoleteSymbols { get; set; }
        public bool SuggestTypesOnObjectCreation { get; set; }
        public bool AddParethesisForNewSuggestions { get; set; }
        public bool SuggestFactoryMethodsOnObjectCreation { get; set; }
        public bool SuggestLocalVariablesFirst { get; set; }

        public bool EnableUnimportedSuggestions => EnableTypesSuggestions || EnableExtensionMethodsSuggestions;
    }
}
