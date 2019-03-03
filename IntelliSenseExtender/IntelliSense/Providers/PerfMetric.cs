using System;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public static class PerfMetric
    {
        public static int TotalTypesCount;
        public static int TotalItemsCount;

        public static long TraverseTypes_SymbolNames;
        public static long TraverseTypes_ExtMethods;

        public static long CreateComplItem_Total => CreateComplItem_Span.Milliseconds;
        public static TimeSpan CreateComplItem_Span;
        public static long CreateComplItem_Props => CreateComplItem_Props_Span.Milliseconds;
        public static TimeSpan CreateComplItem_Props_Span;

        public static long AddToContext;

        public static long Total;

        public static void Reset()
        {
            TraverseTypes_SymbolNames = 0;
            TraverseTypes_ExtMethods = 0;
            CreateComplItem_Span = TimeSpan.Zero;
            CreateComplItem_Props_Span = TimeSpan.Zero;
            Total = 0;
            TotalTypesCount = 0;
            TotalItemsCount = 0;
        }

    }
}
