using IntelliSenseExtender.IntelliSense.Providers;
using IntelliSenseExtender.Tests.CompletionProviders;

namespace IntelliSenseExtender.Tests
{
    internal class Performance : AbstractCompletionProviderTest
    {
        private static void Main(string[] args)
        {
            const string mainSource = @"
                public class Test {
                    public void Method() {
                        /*here*/
                    }
                }";
            const string classFile = @"
                namespace NM
                {
                    public class Class
                    {
                    }
                }";

            var provider = new AggregateTypeCompletionProvider(
    Options_Default,
                  new TypesCompletionProvider(),
                  new ExtensionMethodsCompletionProvider(),
                  new LocalsCompletionProvider(),
                  new NewObjectCompletionProvider(),
                  new EnumCompletionProvider());

            var completions = GetCompletionsAsync(provider, mainSource, classFile, "/*here*/").Result;
        }
    }
}
