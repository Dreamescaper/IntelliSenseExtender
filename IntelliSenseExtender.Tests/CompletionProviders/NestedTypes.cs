using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.IntelliSense.Providers;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace IntelliSenseExtender.Tests.CompletionProviders
{
    [TestFixture]
    public class NestedTypes : AbstractCompletionProviderTest
    {
        private readonly CompletionProvider Provider = new AggregateTypeCompletionProvider(
            Options_Default,
            new NestedTypesCompletionProvider());

        private CompletionProvider Provider_WithOptions(Action<Options.Options> action) =>
            new AggregateTypeCompletionProvider(Options_With(action),
                new NestedTypesCompletionProvider());

        [Test]
        public async Task ShouldAddUsingOnCommit()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        var list = new 
                    }
                }";

            const string classFile = @"
                namespace NM
                {
                    public class ContainingClass
                    {
                        public class NestedClass { }
                    }
                }";

            var document = GetTestDocument(source, classFile);
            var listCompletion = (await GetCompletionsAsync(Provider_WithOptions(o => o.SuggestNestedTypes = true), document, "var list = new "))
                .First(c => Matches(c, "ContainingClass.NestedClass", "NM"));
            listCompletion = CompletionList
                .Create(new TextSpan(source.IndexOf("var list = new "), 0), ImmutableArray.Create(listCompletion))
                .Items[0];
            var changes = await Provider.GetChangeAsync(document, listCompletion, ' ', CancellationToken.None);
            var textWithChanges = (await document.GetTextAsync()).WithChanges(changes.TextChange).ToString();

            Assert.That(NormSpaces(textWithChanges), Is.EqualTo(NormSpaces(@"
                using NM;

                public class Test {
                    public void Method() {
                        var list = new ContainingClass.NestedClass
                    }
                }")));
        }

        [Test]
        public async Task DoNotProvideCompletionsIfTypeNotExpected()
        {
            const string mainSource = @"
                /*0*/
                namespace /*1*/ NM
                {
                    public /*2*/ class Test {
                        public async Task /*3*/ Method() {
                        
                        }
                    }
                }";

            for (int i = 0; i < 4; i++)
            {
                var completions = await GetCompletionsAsync(Provider, mainSource, $"/*{i}*/");
                Assert.That(completions, Is.Empty);
            }
        }

        [Test]
        public async Task SuggestUnimportedNestedTypesIfEnabled()
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
                    public class ContainingClass
                    {
                        public class NestedClass { }
                    }
                }";

            var completions = await GetCompletionsAsync(Provider_WithOptions(o => o.SuggestNestedTypes = true),
                mainSource, classFile, "/*here*/");

            Assert.That(completions, NotContains("NestedClass", "NM"));
            Assert.That(completions, Contains("ContainingClass.NestedClass", "NM"));
        }

        [Test]
        public async Task SuggestImportedNestedTypesIfEnabled()
        {
            const string mainSource = @"
                using NM;

                public class Test {
                    public void Method() {
                        /*here*/
                    }
                }";
            const string classFile = @"
                namespace NM
                {
                    public class ContainingClass
                    {
                        public class NestedClass { }
                    }
                }";

            var completions = await GetCompletionsAsync(Provider_WithOptions(o => o.SuggestNestedTypes = true),
                mainSource, classFile, "/*here*/");

            Assert.That(completions, Contains("ContainingClass.NestedClass"));
        }

        [Test]
        public async Task DoNotSuggestNestedTypesIfDisabled()
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
                    public class ContainingClass
                    {
                        public class NestedClass { }
                    }
                }";

            var completions = await GetCompletionsAsync(Provider_WithOptions(o => o.SuggestNestedTypes = false),
                mainSource, classFile, "/*here*/");

            Assert.That(completions, NotContains("NestedClass", "NM"));
            Assert.That(completions, NotContains("ContainingClass.NestedClass", "NM"));
        }

        [Test]
        public async Task DoNotSuggestNestedTypeIfStaticImportPresent()
        {
            const string mainSource = @"
                using static NM.ContainingClass;
            
                public class Test {
                    public void Method() {
                        /*here*/
                    }
                }";
            const string classFile = @"
                namespace NM
                {
                    public class ContainingClass
                    {
                        public class NestedClass { }
                    }
                }";

            var completions = await GetCompletionsAsync(Provider_WithOptions(o => o.SuggestNestedTypes = true),
                mainSource, classFile, "/*here*/");
            var completionNames = completions.Select(c => c.DisplayText).ToArray();

            Assert.That(completionNames, Has.None.Contains("NestedClass"));
        }

        private static string NormSpaces(string str)
        {
            return Regex.Replace(@"\s+", str, " ").Trim();
        }
    }
}