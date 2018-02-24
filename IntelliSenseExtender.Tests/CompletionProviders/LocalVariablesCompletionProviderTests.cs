using System.Linq;
using IntelliSenseExtender.IntelliSense.Providers;
using NUnit.Framework;

namespace IntelliSenseExtender.Tests.CompletionProviders
{
    [TestFixture]
    public class LocalVariablesCompletionProviderTests : AbstractCompletionProviderTest
    {
        [Test]
        public void SuggestLocalVariablesForMethodsParameters()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        int i = 0;
                        IntMethod(
                    }

                    public void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("i"));
        }

        [Test]
        public void SuggestInlineDeclaredLocalVariables()
        {
            const string source = @"
                public class Test {
                    public void Method(object o) {
                        if(o is string strVar)
                        {
                            StrMethod(
                        }
                    }

                    public void StrMethod(string var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "StrMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("strVar"));
        }

        [Test]
        public void SuggestForeachVariable()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        var intArray = new int[]{1,2,3};
                        foreach(var i in intArray)
                        {
                            IntMethod(
                        }
                    }

                    public void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("i"));
        }

        [Test]
        public void SuggestForVariable()
        {
            const string source = @"
                public class Test {
                    public void Method()
                    {
                        for(int i = 0; i < 5; i++)
                        {
                            IntMethod(
                        }
                    }

                    public void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("i"));
        }

        [Test]
        public void SuggestUsingVariable()
        {
            const string source = @"
                using System.IO;

                public class Test {
                    public void Method()
                    {
                        using (var v = new StreamReader(""))
                        {
                            SrMethod(
                        }
                    }

                    public void SrMethod(StreamReader var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "SrMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("v"));
        }

        [Test]
        public void SuggestMethodParameters()
        {
            const string source = @"
                public class Test {
                    public void Method(int i) {
                        IntMethod(
                    }

                    public void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("i"));
        }

        [Test]
        public void SuggestLabmdaParameters()
        {
            const string source = @"
                using System;

                public class Test {
                    public void Method() {
                        Action<int> action = i => IntMethod(
                    }

                    public void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("i"));
        }

        [Test]
        public void SuggestProperties()
        {
            const string source = @"
                public class Test {
                    private int Prop => 0;

                    public void Method() {
                        IntMethod(
                    }

                    public void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("Prop"));
        }

        [Test]
        public void SuggestFields()
        {
            const string source = @"
                public class Test {
                    private int _field = 0;

                    public void Method() {
                        IntMethod(
                    }

                    public void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("_field"));
        }

        [Test]
        public void SuggestProtectedProperties()
        {
            const string source = @"
                using System;

                public class A
                {
                    protected int ProtectedIntProperty { get; }
                }

                public class B : A
                {
                    public void Method() {
                        IntMethod(
                    }

                    public void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("ProtectedIntProperty"));
        }

        [Test]
        public void SuggestAssignableVariables()
        {
            const string source = @"
                public class A
                {
                }

                public class B : A
                {
                }

                public class TestClass
                {
                    public void Method()
                    {
                        B bVar = new B();
                        AMethod(
                    }

                    public void AMethod(A aVar)
                    {
                    }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "AMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("bVar"));
        }

        [Test]
        public void DontSuggestNotSuitableVariables()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        string s = ""str"";
                        IntMethod(
                    }

                    public void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Not.Contain("s"));
        }

        [Test]
        public void DontSuggestPrivatePropertiesFromBaseClass()
        {
            const string source = @"
                using System;

                public class A
                {
                    private int PrivateIntProperty { get; }
                }

                public class B : A
                {
                    public void Method() {
                        IntMethod(
                    }

                    public void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Not.Contain("PrivateIntProperty"));
        }

        [Test]
        public void DontSuggestLocalsOutOfScope()
        {
            const string source = @"
                using System;

                public class A
                {
                    public void Method() {
                        if(true)
                        {
                            int outOfScope = 0;
                        }

                        IntMethod(
                    }

                    public void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Not.Contain("outOfScope"));
        }

        [Test]
        public void DontSuggestLocalsDefinedLater()
        {
            const string source = @"
                using System;

                public class A
                {
                    public void Method() {
                        IntMethod(
                        
                        int undefinedSoFar = 0;
                    }

                    public void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Not.Contain("undefinedSoFar"));
        }

        [Test]
        public void DontSuggestForVariableOutOfFor()
        {
            const string source = @"
                public class Test {
                    public void Method()
                    {
                        for(int i = 0; i < 5; i++)
                        {
                        }
                        IntMethod(
                    }

                    public void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Not.Contain("i"));
        }

        [Test]
        public void DontSuggestForeachVariableOutOfForeach()
        {
            const string source = @"
                public class Test {
                    public void Method()
                    {
                        var intArray = new int[] {1,2,3};
                        foreach(var i in intArray)
                        { }

                        IntMethod(
                    }

                    public void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Not.Contain("i"));
        }

        [Test]
        public void DontSuggestInstanceMembersInStaticMethod()
        {
            const string source = @"
                public class Test {
                    private int _field = 0;

                    public static void Method() {
                        IntMethod(
                    }

                    public static void IntMethod(int var){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "IntMethod(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Not.Contain("_field"));
        }

        [Test]
        public void DontSuggestAnythingInArbitraryContext()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        int i = 0;
                    }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var document = GetTestDocument(source);

            for (int i = 0; i < source.Length; i++)
            {
                var context = GetContext(document, provider, i);
                provider.ProvideCompletionsAsync(context).Wait();
                var completions = GetCompletions(context);

                Assert.That(completions, Is.Empty);
            }
        }

        [Test]
        public void SuggestCorrectArgumentType()
        {
            const string source = @"
                public class Test {
                    private int _intField = 0;
                    private string _stringField = "";
                    private bool _boolField = false;

                    public void Method() {
                        MultiMethod(_intField, 
                    }

                    public void MultiMethod(int intVar, string strVar, bool boolVar){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "MultiMethod(_intField, ");
            var completionsNames = completions.Select(completion => completion.DisplayText);

            Assert.That(completionsNames, Does.Not.Contain("_intField"));
            Assert.That(completionsNames, Does.Not.Contain("_boolField"));
            Assert.That(completionsNames, Does.Contain("_stringField"));
        }

        [Test]
        public void SuggestCorrectNamedArgumentType()
        {
            const string source = @"
                public class Test {
                    private int _intField = 0;
                    private string _stringField = "";
                    private bool _boolField = false;

                    public void Method() {
                        MultiMethod(boolVar:  
                    }

                    public void MultiMethod(int intVar, string strVar, bool boolVar){ }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "MultiMethod(boolVar:  ");
            var completionsNames = completions.Select(completion => completion.DisplayText);

            Assert.That(completionsNames, Does.Not.Contain("_intField"));
            Assert.That(completionsNames, Does.Not.Contain("_stringField"));
            Assert.That(completionsNames, Does.Contain("_boolField"));
        }
    }
}
