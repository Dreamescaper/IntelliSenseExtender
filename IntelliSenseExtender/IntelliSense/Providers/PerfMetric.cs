using System;

namespace IntelliSenseExtender.IntelliSense.Providers
{
    public static class PerfMetric
    {
        public static int TotalTypesCount;
        public static int TotalItemsCount;

        public static TimeSpan TraverseTypes;

        public static long CreateComplItem_Total => (long)CreateComplItem_Span.TotalMilliseconds;
        public static TimeSpan CreateComplItem_Span;
        public static long CreateComplItem_Props => (long)CreateComplItem_Props_Span.TotalMilliseconds;
        public static TimeSpan CreateComplItem_Props_Span;

        public static long AddToContext;
        public static long CreateSyntaxContext;
        public static long Total;
        internal static long TypeProviders;
        internal static long SimpleProviders;

        internal static long Locals;
        internal static TimeSpan Types;
        internal static TimeSpan NewObjects;
        internal static TimeSpan ExtMethods;

        public static void Reset()
        {
            TraverseTypes = TimeSpan.Zero;
            CreateComplItem_Span = TimeSpan.Zero;
            CreateComplItem_Props_Span = TimeSpan.Zero;
            Types = TimeSpan.Zero;
            NewObjects = TimeSpan.Zero;
            ExtMethods = TimeSpan.Zero;
            Total = 0;
            TotalTypesCount = 0;
            TotalItemsCount = 0;
            CreateSyntaxContext = 0;
        }

    }
}
