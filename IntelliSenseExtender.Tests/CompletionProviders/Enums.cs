using System.Linq;
using System.Threading.Tasks;
using IntelliSenseExtender.IntelliSense.Providers;
using Microsoft.CodeAnalysis.Completion;
using NUnit.Framework;

namespace IntelliSenseExtender.Tests.CompletionProviders
{
    public class Enums : AbstractCompletionProviderTest
    {
        private readonly CompletionProvider Provider = new AggregateTypeCompletionProvider(
            Options_Default,
            new EnumCompletionProvider());

        [Test]
        public async Task SuggestInferedEnumType()
        {
            const string mainSource = @"
                public class Test {
                    public void Method() {
                        NM.SomeEnum e = /*here*/
                    }
                }";
            const string classFile = @"
                namespace NM
                {
                    public enum SomeEnum
                    {
                        A, B, C
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, mainSource, classFile, "/*here*/");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("SomeEnum  (NM)"));
        }

        [Test]
        public async Task DoNotSuggestInArbitraryContext()
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
                    public enum SomeEnum
                    {
                        A, B, C
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, mainSource, classFile, "/*here*/");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Is.Empty);
        }
    }
}
