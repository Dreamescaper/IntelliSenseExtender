using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.IntelliSense;
using IntelliSenseExtender.IntelliSense.Providers;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace IntelliSenseExtender.Tests.CompletionProviders
{
    [TestFixture]
    public class Types : AbstractCompletionProviderTest
    {
        private readonly CompletionProvider Provider = new AggregateTypeCompletionProvider(
            Options_Default,
            new TypesCompletionProvider());

        private CompletionProvider Provider_WithOptions(Action<Options.Options> action) =>
            new AggregateTypeCompletionProvider(Options_With(action),
                new TypesCompletionProvider());

        [Test]
        public async Task ShouldAddUsingOnCommit()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        var list = new 
                    }
                }";

            var document = GetTestDocument(source);
            var listCompletion = (await GetCompletions(Provider, document, "var list = new "))
                .First(c => c.DisplayText == "List<>  (System.Collections.Generic)");
            listCompletion = CompletionList
                .Create(new TextSpan(source.IndexOf("var list = new "), 0), ImmutableArray.Create(listCompletion))
                .Items[0];
            var changes = await Provider.GetChangeAsync(document, listCompletion, ' ', CancellationToken.None);
            var textWithChanges = (await document.GetTextAsync()).WithChanges(changes.TextChange).ToString();

            Assert.That(NormSpaces(textWithChanges), Is.EqualTo(NormSpaces(@"
                using System.Collections.Generic;

                public class Test {
                    public void Method() {
                        var list = new List
                    }
                }")));
        }

        [Test]
        public void ProvideReferencesCompletions_List()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        var list = new 
                    }
                }";

            var completions = GetCompletions(Provider, source, "var list = new ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("List<>  (System.Collections.Generic)"));
        }

        [Test]
        public void ProvideUserCodeCompletions()
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

            var completions = GetCompletions(Provider, mainSource, classFile, "/*here*/");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("Class  (NM)"));
        }

        [Test]
        public void DoNotProvideCompletionsIfTypeNotExpected()
        {
            const string mainSource = @"
                /*0*/
                namespace /*1*/ NM
                {
                    public /*2*/ class Test {
                        public void /*3*/ Method() {
                        
                        }
                    }
                }";

            for (int i = 0; i < 4; i++)
            {
                var completions = GetCompletions(Provider, mainSource, $"/*{i}*/");
                Assert.That(completions, Is.Empty);
            }
        }

        [Test]
        public void ShorterTypeGoesFirst()
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
                    public class CoolClass
                    {
                    }
                    public class CoolClassWithLongerName
                    {
                    }
                }";

            var completions = GetCompletions(Provider, mainSource, classFile, "/*here*/")
                .OrderBy(compl => compl.SortText, StringComparer.Ordinal)
                .ToList();

            int coolClassIndex = completions.FindIndex(i =>
                i.Properties[CompletionItemProperties.FullSymbolName].EndsWith("CoolClass"));
            int coolClassWithLongerNameIndex = completions.FindIndex(i =>
                i.Properties[CompletionItemProperties.FullSymbolName].EndsWith("CoolClassWithLongerName"));

            Assert.That(coolClassIndex < coolClassWithLongerNameIndex);
        }

        [Test]
        public void SuggestUnimportedNestedTypesIfEnabled()
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

            var completions = GetCompletions(Provider_WithOptions(o => o.SuggestNestedTypes = true),
                mainSource, classFile, "/*here*/");
            var completionsNames = completions.Select(completion => completion.DisplayText);

            Assert.That(completionsNames, Does.Not.Contain("NestedClass  (NM)"));
            Assert.That(completionsNames, Does.Contain("ContainingClass.NestedClass  (NM)"));
        }

        [Test]
        public void SuggestImportedNestedTypesIfEnabled()
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

            var completions = GetCompletions(Provider_WithOptions(o => o.SuggestNestedTypes = true),
                mainSource, classFile, "/*here*/");
            var completionsNames = completions.Select(completion => completion.DisplayText);

            Assert.That(completionsNames, Does.Contain("ContainingClass.NestedClass"));
        }

        [Test]
        public void DoNotSuggestNestedTypesIfDisabled()
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

            var completions = GetCompletions(Provider_WithOptions(o => o.SuggestNestedTypes = false),
                mainSource, classFile, "/*here*/");
            var completionsNames = completions.Select(completion => completion.DisplayText);

            Assert.That(completionsNames, Does.Not.Contain("NestedClass  (NM)"));
            Assert.That(completionsNames, Does.Not.Contain("ContainingClass.NestedClass  (NM)"));
        }

        [Test]
        public void DoNotProvideObsoleteTypes()
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
                    [System.Obsolete]
                    public class Class
                    {
                    }
                }";

            var completions = GetCompletions(Provider, mainSource, classFile, "/*here*/");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Not.Contain("Class  (NM)"));
        }

        [Test]
        public void DoNotSuggestTypesInGlobalNamespace()
        {
            const string mainSource = @"
                public class Test {
                    public void Method() {
                        /*here*/
                    }
                }";
            const string classFile = @"
                public class GlobalNsClass
                {
                }";

            var completions = GetCompletions(Provider, mainSource, classFile, "/*here*/");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Has.None.Contains("GlobalNsClass"));
        }

        private static string NormSpaces(string str)
        {
            return Regex.Replace(@"\s+", str, " ").Trim();
        }
    }
}
