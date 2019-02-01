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
            var listCompletion = (await GetCompletionsAsync(Provider, document, "var list = new "))
                .First(c => Matches(c, "List<>", "System.Collections.Generic"));
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
        public async Task ProvideReferencesCompletions_List()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        var list = new 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "var list = new ");

            Assert.That(completions, Contains("List<>", "System.Collections.Generic"));
            Assert.That(completions, Contains("List<>", "System.Collections.Generic"));
        }

        [Test]
        public async Task ProvideUserCodeCompletions()
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

            var completions = await GetCompletionsAsync(Provider, mainSource, classFile, "/*here*/");
            Assert.That(completions, Contains("Class", "NM"));
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
        public async Task ShorterTypeGoesFirst()
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

            var completions = (await GetCompletionsAsync(Provider, mainSource, classFile, "/*here*/"))
                .OrderBy(compl => compl.SortText, StringComparer.Ordinal)
                .ToList();

            int coolClassIndex = completions.FindIndex(i =>
                i.Properties[CompletionItemProperties.FullSymbolName].EndsWith("CoolClass"));
            int coolClassWithLongerNameIndex = completions.FindIndex(i =>
                i.Properties[CompletionItemProperties.FullSymbolName].EndsWith("CoolClassWithLongerName"));

            Assert.That(coolClassIndex < coolClassWithLongerNameIndex);
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

        [Test]
        public async Task DoNotProvideObsoleteTypes()
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

            var completions = await GetCompletionsAsync(Provider, mainSource, classFile, "/*here*/");
            Assert.That(completions, NotContains("Class", "NM"));
        }

        [Test]
        public async Task DoNotSuggestTypesInGlobalNamespace()
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

            var completions = await GetCompletionsAsync(Provider, mainSource, classFile, "/*here*/");
            var completionNames = completions.Select(c => c.DisplayText).ToArray();

            Assert.That(completionNames, Has.None.Contains("GlobalNsClass"));
        }

        [Test]
        public async Task DoNotSuggestIfAlreadyImported_OutsideNamespace()
        {
            const string mainSource = @"
                using System.Collections.Generic;

                namespace TestNamespace
                {
                    public class Test
                    {
                        public void Method()
                        {
                            /*here*/
                        }
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, mainSource, "/*here*/");
            Assert.That(completions, NotContains("List<>", "System.Collections.Generic"));
        }

        [Test]
        public async Task DoNotSuggestIfAlreadyImported_InsideRegion()
        {
            const string mainSource = @"
                #region Test

                using System.Collections.Generic;

                namespace TestNamespace
                {
                    public class Test
                    {
                        public void Method()
                        {
                            /*here*/
                        }
                    }
                }

                #endregion";

            var completions = await GetCompletionsAsync(Provider, mainSource, "/*here*/");
            Assert.That(completions, NotContains("List<>", "System.Collections.Generic"));
        }

        [Test]
        public async Task DoNotSuggestIfAlreadyImported_InsideNamespace()
        {
            const string mainSource = @"
                namespace TestNamespace
                {
                    using System.Collections.Generic;

                    public class Test
                    {
                        public void Method()
                        {
                            /*here*/
                        }
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, mainSource, "/*here*/");
            Assert.That(completions, NotContains("List<>", "System.Collections.Generic"));
        }

        [Test]
        public async Task DoNotSuggestIfAlreadyImported_InsideNamespace_PartialPath()
        {
            const string mainSource = @"
                namespace System.Collections
                {
                    using Generic;

                    public class Test
                    {
                        public void Method()
                        {
                            /*here*/
                        }
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, mainSource, "/*here*/");
            Assert.That(completions, NotContains("List<>", "System.Collections.Generic"));
        }

        [Test]
        public async Task DoNotSuggestIfAlreadyImported_ParentNamespace()
        {
            const string mainSource = @"
                namespace System.Collections.Generic.Some.Child.Name.Space
                {
                    public class Test
                    {
                        public void Method()
                        {
                            /*here*/
                        }
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, mainSource, "/*here*/");
            Assert.That(completions, NotContains("List<>", "System.Collections.Generic"));
        }

        [Test]
        public async Task DoNotSuggestIfAliasUsingIsPresent()
        {
            const string mainSource = @"
                using ContainingClass = NM.ContainingClass;
            
                public class Test
                {
                    public void Method()
                    {
                        /*here*/
                    }
                }";
            const string classFile = @"
                namespace NM
                {
                    public class ContainingClass
                    {
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, mainSource, classFile, "/*here*/");

            Assert.That(completions, NotContains("ContainingClass", "NM"));
        }

        private static string NormSpaces(string str)
        {
            return Regex.Replace(@"\s+", str, " ").Trim();
        }
    }
}