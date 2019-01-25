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
        public async Task SuggestInferredEnumType()
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
        public async Task SuggestInferredEnumType_Nullable()
        {
            const string mainSource = @"
                public class Test {
                    public void Method() {
                        NM.SomeEnum? e = /*here*/
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
        public async Task SuggestInferredEnumType_Nested()
        {
            const string mainSource = @"
                public class Test {
                    public void Method() {
                        NM.ContainingClass.SomeEnum? e = /*here*/
                    }
                }";
            const string classFile = @"
                namespace NM
                {
                    public class ContainingClass
                    {
                        public enum SomeEnum
                        {
                            A, B, C
                        }
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, mainSource, classFile, "/*here*/");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("ContainingClass.SomeEnum  (NM)"));
        }

        [Test]
        public async Task SuggestInferredEnumType_Nested_WithStaticImport()
        {
            const string mainSource = @"
                using static NM.ContainingClass;

                public class Test {
                    public void Method() {
                        var e = SomeEnum.A;
                        e == /*here*/
                    }
                }";
            const string classFile = @"
                namespace NM
                {
                    public class ContainingClass
                    {
                        public enum SomeEnum
                        {
                            A, B, C
                        }
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, mainSource, classFile, "/*here*/");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("SomeEnum"));
            Assert.That(completionsNames, Does.Not.Contain("ContainingClass.SomeEnum  (NM)"));
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
