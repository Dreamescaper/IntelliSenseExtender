using System;
using System.Linq;
using IntelliSenseExtender.IntelliSense;
using IntelliSenseExtender.IntelliSense.Providers;
using Microsoft.CodeAnalysis.Completion;
using NUnit.Framework;

namespace IntelliSenseExtender.Tests.CompletionProviders
{
    [TestFixture]
    public class Types : AbstractCompletionProviderTest
    {
        private readonly CompletionProvider Provider = new AggregateTypeCompletionProvider(
            Options_Default,
            new TypesCompletionProvider());

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
                public /*0*/ class Test {
                    public void /*1*/ Method() {
                        
                    }
                }";
            const string classFile = @"
                namespace NM
                {
                    public class Class
                    {
                    }
                }";

            for (int i = 0; i < 3; i++)
            {
                var completions = GetCompletions(Provider, mainSource, classFile, $"/*{i}*/");
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
                i.Properties[CompletionItemProperties.SymbolName] == "CoolClass");
            int coolClassWithLongerNameIndex = completions.FindIndex(i =>
                i.Properties[CompletionItemProperties.SymbolName] == "CoolClassWithLongerName");

            Assert.That(coolClassIndex < coolClassWithLongerNameIndex);
        }

        [Test]
        public void CorrectNestedTypesNaming()
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

            var completions = GetCompletions(Provider, mainSource, classFile, "/*here*/");
            var completionsNames = completions.Select(completion => completion.DisplayText);

            Assert.That(completionsNames, Does.Not.Contain("NestedClass  (NM)"));
            Assert.That(completionsNames, Does.Contain("ContainingClass.NestedClass  (NM)"));
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
    }
}
