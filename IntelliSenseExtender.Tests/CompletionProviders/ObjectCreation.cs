using System.Linq;
using IntelliSenseExtender.IntelliSense.Providers;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace IntelliSenseExtender.Tests.CompletionProviders
{
    [TestFixture]
    public class ObjectCreation : AbstractCompletionProviderTest
    {
        private readonly CompletionProvider Provider = new AggregateTypeCompletionProvider(
            Options_Default,
            new NewObjectCompletionProvider());

        [Test]
        public void SuggestInterfaceImplementation_LocalVariable()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test {
                    public void Method() {
                        IList<string> list = 
                    }
                }";

            var completions = GetCompletions(Provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("new List<string>()"));
        }

        [Test]
        public void SuggestInterfaceImplementation_Member_InPlace()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test {
                     private IList<string> list = 
                }";

            var completions = GetCompletions(Provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("new List<string>()"));
        }

        [Test]
        public void SuggestInterfaceImplementation_Member_InConstructor()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test 
                {
                     public IList<string> List {get;}

                     public Test()
                     {
                        List = 
                     }
                }";

            var completions = GetCompletions(Provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("new List<string>()"));
        }

        [Test]
        public void SuggestInterfaceImplementation_MethodParameter()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test 
                {
                     public bool DoSomething(ICollection<int> par) => true;

                     public Test()
                     {
                         int res = DoSomething(
                     }
                }";

            var completions = GetCompletions(Provider, source, "int res = DoSomething(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("new List<int>()"));
        }

        [Test]
        public void SuggestInterfaceImplementation_ConstructorParameter()
        {
            const string source = @"
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

            var completions = GetCompletions(Provider, source, "new Test1(");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("new List<int>()"));
        }

        [Test]
        public void SuggestInterfaceImplementation_AfterNewKeyword()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test {
                    public void Method() {
                        IList<string> list = new 
                    }
                }";

            var completions = GetCompletions(Provider, source, " = new ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("List<string>"));
        }

        [Test]
        public void SuggestInterfaceImplementation_UnimportedTypes()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        System.Collections.Generic.IList<string> list =  
                    }
                }";

            var completions = GetCompletions(Provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames,
                Does.Contain("new List<string>()  (System.Collections.Generic)"));
        }

        [Test]
        public void DoNotSuggestGenericTypesIfConstraintNotSatisfied()
        {
            const string source = @"
                using System;
                using System.Collections.Generic;

                namespace NM
                {
                    public class Test
                    {
                        public void Method()
                        {
                            IList<string> list = 
                        }
                    }

                    public class TContraints<T> : List<T> where T : SomeClass
                    {

                    }

                    public class SomeClass
                    {
                    }
                }";

            var completions = GetCompletions(Provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames,
                Does.Not.Contain("new TContraints<string>()"));
        }

        [Test]
        public void DoNotSuggestNewNullableEnums()
        {
            const string source = @"
                using System;

                namespace NM
                {
                    public class Test
                    {
                        private SomeEnum? enumField;

                        public void Method()
                        {
                            enumField = 
                        }
                    }

                    public enum SomeEnum { A, B, C}
                }";

            var completions = GetCompletions(Provider, source, "enumField = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames,
                Does.Not.Contain("new SomeEnum?()"));
        }

        [Test]
        public void SuggestGenericTypesIfTypeConstraintSatisfied()
        {
            const string source = @"
                using System;
                using System.Collections.Generic;

                namespace NM
                {
                    public class Test
                    {
                        public void Method()
                        {
                            IList<SomeClassDerived> list = 
                        }
                    }

                    public class TContraints<T> : List<T> where T : SomeClass
                    {

                    }

                    public class SomeClass
                    {
                    }

                    public class SomeClassDerived: SomeClass
                    {
                    }
                }";

            var completions = GetCompletions(Provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames,
                Does.Contain("new TContraints<SomeClassDerived>()"));
        }

        [Test]
        public void SuggestArrayInitialyzer()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        int[] =  
                    }
                }";

            var completions = GetCompletions(Provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("new [] {}"));
        }

        [Test]
        public void SuggestFactoryMethods()
        {
            const string source = @"
                using System;
                public class Test {
                    public void DoSmth()
                    {
                        TimeSpan ts = 
                    }
                }";

            var completions = GetCompletions(Provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("TimeSpan.FromSeconds"));
        }

        [Test]
        public void DoNotSuggestStaticMethodsForBuiltInTypes()
        {
            const string source = @"
                using System;
                public class Test {
                    public void DoSmth()
                    {
                        string str = 
                    }
                }";

            var completions = GetCompletions(Provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Not.Contain("String.Concat"));
        }

        [Test]
        public void SuggestStaticProperties()
        {
            const string source = @"
                using System;
                public class Test {
                    public static bool DoSmth(Test testInstance)
                    {
                        DateTime dt = 
                    }
                }";

            var completions = GetCompletions(Provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("DateTime.Now"));
        }

        [Test]
        public void SuggestListInitialyzer()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test {
                    public void Method() {
                        List<int> lst = 
                    }
                }";

            var completions = GetCompletions(Provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Contain("new List<int> {}"));
        }

        [TestCase("int", "Int32")]
        [TestCase("double", "Double")]
        [TestCase("string", "String")]
        [TestCase("IComparable", "Int32")]
        public void DoNotSuggestPrimitiveTypesConstructors(string shortName, string typeName)
        {
            var source = @"
                using System;

                public class Test {
                    public void Method() {" +
                       $"{shortName} v = " +
                    @"}
                }";

            var completions = GetCompletions(Provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);
            Assert.That(completionsNames, Does.Not.Contain($"new {shortName}()"));
            Assert.That(completionsNames, Does.Not.Contain($"new {typeName}()"));
        }

        [Test]
        public void SuggestTrueFalseForBool()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                       bool b = 
                    }
                }";

            var completions = GetCompletions(Provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);

            Assert.That(completionsNames, Does.Contain("true"));
            Assert.That(completionsNames, Does.Contain("false"));
        }

        [Test]
        public void DoNotSuggestAnythingIfNotApplicable()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test {
                    public Test(List<string> lst) 
                    { }

                    public void Method() 
                    { }

                    public static void DoSmth(Test testInstance)
                    {
                        testInstance.Method();
                        var v1 = new
                    }
                }";

            var document = GetTestDocument(source);

            for (int i = 0; i < source.Length; i++)
            {
                var context = GetContext(document, Provider, i);
                Provider.ProvideCompletionsAsync(context).Wait();
                var completions = GetCompletions(context);

                Assert.That(completions, Is.Empty);
            }
        }

        [Test]
        public void DoNotSuggestAnythingForWrongMemberName()
        {
            const string source = @"
                internal class Test
                {
                    public object this[string key]
                    {
                        set
                        {
                            this.name = 
                        }
                    }
                }";

            var completions = GetCompletions(Provider, source, " = ");
            var completionsNames = completions.Select(completion => completion.DisplayText);

            Assert.That(completionsNames, Is.Empty);
        }

        [Test]
        public void SuggestOnReturn()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test {
                    public List<int> Method() {
                        return 
                    }
                }";

            var completions = GetCompletions(Provider, source, "return ");
            var completionsNames = completions.Select(completion => completion.DisplayText);

            Assert.That(completionsNames, Does.Contain("new List<int>()"));
        }

        [Test]
        public void TriggerCompletionAfterAssignment()
        {
            const string source = @"
                public class Test {
                    public static bool DoSmth(Test testInstance)
                    {
                        Test v1 = 
                    }
                }";

            bool triggerCompletion = Provider.ShouldTriggerCompletion(
                text: SourceText.From(source),
                caretPosition: source.IndexOf(" = ") + 3,
                trigger: CompletionTrigger.CreateInsertionTrigger(' '),
                options: null);
            Assert.That(triggerCompletion);
        }

        [Test]
        public void TriggerCompletionNewKeyword()
        {
            const string source = @"
                public class Test {
                    public static bool DoSmth(Test testInstance)
                    {
                        Test v1 = new 
                    }
                }";

            bool triggerCompletion = Provider.ShouldTriggerCompletion(
                text: SourceText.From(source),
                caretPosition: source.IndexOf("new ") + 4,
                trigger: CompletionTrigger.CreateInsertionTrigger(' '),
                options: null);
            Assert.That(triggerCompletion);
        }
    }
}
