using System.Linq;
using IntelliSenseExtender.IntelliSense.Providers;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace IntelliSenseExtender.Tests.CompletionProviders
{
    [TestFixture]
    public class ObjectCreationProviderTests : AbstractCompletionProviderTest
    {
        [Test]
        public void SuggestInterfaceImplementation_LocalVariable()
        {
            var source = @"
                using System.Collections.Generic;

                public class Test {
                    public void Method() {
                        IList<string> list = 
                    }
                }";

            var provider = new NewObjectCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("new List<string>()"));
        }

        [Test]
        public void SuggestInterfaceImplementation_Member_InPlace()
        {
            var source = @"
                using System.Collections.Generic;

                public class Test {
                     private IList<string> list = 
                }";

            var provider = new NewObjectCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("new List<string>()"));
        }

        [Test]
        public void SuggestInterfaceImplementation_Member_InConstructor()
        {
            var source = @"
                using System.Collections.Generic;

                public class Test 
                {
                     public IList<string> List {get;}

                     public Test()
                     {
                        List = 
                     }
                }";

            var provider = new NewObjectCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("new List<string>()"));
        }

        [Test]
        public void SuggestInterfaceImplementation_MethodParameter()
        {
            var source = @"
                using System.Collections.Generic;

                public class Test 
                {
                     public bool DoSomething(ICollection<int> par) => true;

                     public Test()
                     {
                         int res = DoSomething(
                     }
                }";

            var provider = new NewObjectCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "int res = DoSomething(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("new List<int>()"));
        }

        [Test]
        public void SuggestInterfaceImplementation_ConstructorParameter()
        {
            var source = @"
                using System.Collections.Generic;

                public class Test1
                {
                     public Test1(ICollection<int> par)
                     { }
                }
                
                public class Test2
                {
                    void DoSomething()
                    {
                        var test1 = new Test1(
                    }
                }";

            var provider = new NewObjectCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, "new Test1(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("new List<int>()"));
        }

        [Test]
        public void SuggestInterfaceImplementation_AfterNewKeyword()
        {
            var source = @"
                using System.Collections.Generic;

                public class Test {
                    public void Method() {
                        IList<string> list = new 
                    }
                }";

            var provider = new NewObjectCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, " = new ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("List<string>"));
        }

        [Test]
        public void SuggestInterfaceImplementation_UnimportedTypes()
        {
            var source = @"
                public class Test {
                    public void Method() {
                        System.Collections.Generic.IList<string> list =  
                    }
                }";

            var provider = new NewObjectCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames,
                Does.Contain("new List<string>()  (System.Collections.Generic)"));
        }

        [Test]
        public void SuggestArrayInitialyzer()
        {
            var source = @"
                public class Test {
                    public void Method() {
                        int[] =  
                    }
                }";

            var provider = new NewObjectCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("new [] {}"));
        }

        [Test]
        public void SuggestListInitialyzer()
        {
            var source = @"
                using System.Collections.Generic;

                public class Test {
                    public void Method() {
                        List<int> lst = 
                    }
                }";

            var provider = new NewObjectCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("new List<int> {}"));
        }

        [TestCase("int", "Int32")]
        [TestCase("double", "Double")]
        [TestCase("string", "String")]
        public void DoNotSuggestPrimitiveTypesConstructors(string shortName, string typeName)
        {
            var source = @"
                using System;

                public class Test {
                    public void Method() {" +
                       $"{shortName} v = " +
                    @"}
                }";

            var provider = new NewObjectCompletionProvider(Options_Default);
            var completions = GetCompletions(provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Not.Contain($"new {shortName}()"));
            Assert.That(completionsNames, Does.Not.Contain($"new {typeName}()"));
        }

        [Test]
        public void DoNotSuggestAnythingIfNotApplicable()
        {
            var source = @"
                using System.Collections.Generic;

                public class Test {
                    public Test(List<string> lst) 
                    { }

                    public void Method() 
                    { }

                    public static bool DoSmth(Test testInstance)
                    {
                        testInstance.Method();
                        var v1 = new
                        return true;
                    }
                }";

            var provider = new NewObjectCompletionProvider(Options_Default);
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
        public void TriggerCompletionAfterAssignment()
        {
            var source = @"
                public class Test {
                    public static bool DoSmth(Test testInstance)
                    {
                        Test v1 = 
                    }
                }";

            var provider = new NewObjectCompletionProvider(Options_Default);
            bool triggerCompletion = provider.ShouldTriggerCompletion(
                text: SourceText.From(source),
                caretPosition: source.IndexOf(" = ") + 3,
                trigger: CompletionTrigger.CreateInsertionTrigger(' '),
                options: null);
            Assert.That(triggerCompletion);
        }

        [Test]
        public void TriggerCompletionNewKeyword()
        {
            var source = @"
                public class Test {
                    public static bool DoSmth(Test testInstance)
                    {
                        Test v1 = new 
                    }
                }";

            var provider = new NewObjectCompletionProvider(Options_Default);
            bool triggerCompletion = provider.ShouldTriggerCompletion(
                text: SourceText.From(source),
                caretPosition: source.IndexOf("new ") + 4,
                trigger: CompletionTrigger.CreateInsertionTrigger(' '),
                options: null);
            Assert.That(triggerCompletion);
        }
    }
}
