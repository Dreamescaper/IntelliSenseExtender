namespace IntelliSenseExtender.IntelliSense
{
    public static class Sorting
    {
        public const int Default = -1;
        public const int Last = -2;

        public const int NewSuggestion_MatchingName = 3;
        public const int NewSuggestion_Default = 4;
        public const int NewSuggestion_Unimported = 5;
        public const int NewSuggestion_CollectionInitializer = 3;
        public const int NewSuggestion_Literal = 1;
        public const int NewSuggestion_FactoryMethod = 4;

        public const int Suitable_LambdaParameters = 1;
        public const int Suitable_Locals = 2;
        public const int Suitable_MethodParameters = 3;
        public const int Suitable_TypeMembers = 4;
        public const int Suitable_Enum = 5;
    }
}
