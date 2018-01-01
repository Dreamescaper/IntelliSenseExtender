using System;
using System.Linq;
using IntelliSenseExtender.IntelliSense;
using IntelliSenseExtender.IntelliSense.Providers;
using NUnit.Framework;

namespace IntelliSenseExtender.Tests.CompletionProviders
{
    [TestFixture]
    public class UnimportedCompletionProviderTests : AbstractCompletionProviderTest
    {
        #region Types

        [Test]
        public void Types_ProvideReferencesCompletions_List()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        var list = new 
                    }
                }";

            var provider = new UnimportedCSharpCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "var list = new ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("List<>  (System.Collections.Generic)"));
        }

        [Test]
        public void Types_ProvideUserCodeCompletions()
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

            var provider = new UnimportedCSharpCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, mainSource, classFile, "/*here*/");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("Class  (NM)"));
        }

        [Test]
        public void Types_DoNotProvideCompletionsIfTypeNotExpected()
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

            var provider = new UnimportedCSharpCompletionProvider(Options_TypesOnly);

            for (int i = 0; i < 3; i++)
            {
                var completions = GetCompletions(provider, mainSource, classFile, $"/*{i}*/");
                Assert.That(completions, Is.Empty);
            }
        }

        [Test]
        public void Types_ShorterTypeGoesFirst()
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

            var provider = new UnimportedCSharpCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, mainSource, classFile, "/*here*/")
                .OrderBy(compl => compl.SortText, StringComparer.Ordinal)
                .ToList();

            int coolClassIndex = completions.FindIndex(i =>
                i.Properties[CompletionItemProperties.SymbolName] == "CoolClass");
            int coolClassWithLongerNameIndex = completions.FindIndex(i =>
                i.Properties[CompletionItemProperties.SymbolName] == "CoolClassWithLongerName");

            Assert.That(coolClassIndex < coolClassWithLongerNameIndex);
        }

        #endregion

        #region Extension Methods

        [Test]
        public void ExtensionMethods_ProvideReferencesCompletions_Linq()
        {
            const string source = @"
                using System.Collections.Generic;
                public class Test {
                    public void Method() {
                        var list = new List<string>();
                        list.
                    }
                }";

            var provider = new UnimportedCSharpCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "list.");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("Select<>  (System.Linq)"));
        }

        [Test]
        public void ExtensionMethods_ProvideUserCodeCompletions()
        {
            const string mainSource = @"
                public class Test {
                    public void Method() {
                        object obj = null;
                        obj.
                    }
                }";
            const string extensionsFile = @"
                namespace NM
                {
                    public static class ObjectExtensions
                    {
                        public static void Do(this object var)
                        { }
                    }
                }";

            var provider = new UnimportedCSharpCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, mainSource, extensionsFile, "obj.");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("Do  (NM)"));
        }

        [Test]
        public void ExtensionMethods_ProvideCompletionsForLiterals()
        {
            const string mainSource = @"
                public class Test {
                    public void Method() {
                        111.
                    }
                }";
            const string extensionsFile = @"
                namespace NM
                {
                    public static class ObjectExtensions
                    {
                        public static void Do(this object var)
                        { }
                    }
                }";

            var provider = new UnimportedCSharpCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, mainSource, extensionsFile, "111.");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("Do  (NM)"));
        }

        [Test]
        public void ExtensionMethods_DoNotProvideCompletionsIfMemberIsNotAccessed()
        {
            const string source = @"
                using System;
                namespace A{
                    class CA
                    {
                        [System.Obsolete]
                        public void MA(int par)
                        {
                            var a = 0;
                        }
                    }
                }
                namespace B{
                    static class B{
                        public static void ExtIntM(this int par)
                        { }
                    }
                }";
            const string extensionsFile = @"
                namespace NM
                {
                    public static class ObjectExtensions
                    {
                        public static void Do(this object var)
                        { }
                    }
                }";

            var provider = new UnimportedCSharpCompletionProvider(Options_ExtensionMethodsOnly);
            var document = GetTestDocument(source, extensionsFile);

            for (int i = 0; i < source.Length; i++)
            {
                var context = GetContext(document, provider, i);
                provider.ProvideCompletionsAsync(context).Wait();
                var completions = GetCompletions(context);

                Assert.That(completions, Is.Empty);
            }
        }

        [Test]
        public void ExtensionMethods_DoNotProvideCompletionsWhenTypeIsAccessed()
        {
            const string mainSource = @"
                public class Test {
                    public void Method() {
                        object.
                    }
                }";
            const string extensionsFile = @"
                namespace NM
                {
                    public static class ObjectExtensions
                    {
                        public static void Do(this object var)
                        { }
                    }
                }";

            var provider = new UnimportedCSharpCompletionProvider(Options_ExtensionMethodsOnly);
            var completions = GetCompletions(provider, mainSource, extensionsFile, "object.");
            Assert.That(completions, Is.Empty);
        }

        [Test]
        public void ExtensionMethods_DoNotProvideObsolete()
        {
            const string mainSource = @"
                public class Test {
                    public void Method() {
                        object obj = null;
                        obj.
                    }
                }";
            const string extensionsFile = @"
                namespace NM
                {
                    [System.Obsolete]
                    public static class ObjectExtensions1
                    {
                        public static void Do1(this object var)
                        { }
                    }

                    public static class ObjectExtensions2
                    {
                        [System.Obsolete]
                        public static void Do2(this object var)
                        { }
                    }
                }";

            var provider = new UnimportedCSharpCompletionProvider(Options_ExtensionMethodsOnly);
            var completions = GetCompletions(provider, mainSource, extensionsFile, "obj.");
            Assert.That(completions, Does.Not.Contain("Do1  (NM)"));
            Assert.That(completions, Does.Not.Contain("Do2  (NM)"));
        }

        #endregion
    }
}
