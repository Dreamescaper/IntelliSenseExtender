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
        public void DontSuggestUndefinedLocals()
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
        public void DontSuggestAnythingInArbitraryContext()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        int i = 0;
                        /* here */
                    }
                }";

            var provider = new LocalsCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "/* here */");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Is.Empty);
        }
    }
}
