using System;
using System.Threading.Tasks;
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

        private CompletionProvider Provider_WithOptions(Action<Options.Options> action) =>
            new AggregateTypeCompletionProvider(Options_With(action),
                new NewObjectCompletionProvider());

        [Test]
        public async Task SuggestNonGenericInterfaceImplementation()
        {
            const string source = @"
                using System.Collections;

                public class Test {
                    public void Method() {
                        IList list = 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, Contains("new ArrayList()"));
        }

        [Test]
        public async Task SuggestInterfaceImplementation_LocalVariable()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test {
                    public void Method() {
                        IList<string> list = 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, Contains("new List<string>()"));
        }

        [Test]
        public async Task SuggestInterfaceImplementation_Member_InPlace()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test {
                     private IList<string> list = 
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, Contains("new List<string>()"));
        }

        [Test]
        public async Task SuggestInterfaceImplementation_Member_InConstructor()
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

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, Contains("new List<string>()"));
        }

        [Test]
        public async Task SuggestInterfaceImplementation_MethodParameter()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test 
                {
                     public bool DoSomething(ICollection<int> par) => true;

                     public Test()
                     {
                         bool res = DoSomething(
                     }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "bool res = DoSomething(");
            Assert.That(completions, Contains("new List<int>()"));
        }

        [Test]
        public async Task DoNotSuggestOutMethodParameter()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test 
                {
                    public bool DoSomething(out List<int> par)
                    {
                        par = new List<int>();
                        return true;
                    }

                     public Test()
                     {
                         bool res = DoSomething(
                     }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "bool res = DoSomething(");
            Assert.That(completions, NotContains("new List<int>()"));
        }

        [Test]
        public async Task DoNotSuggestRefMethodParameter()
        {
            const string source = @"
                using System;

                public class Test 
                {
                    public bool DoSomething(ref DateTime par)
                    {
                        return true;
                    }

                     public Test()
                     {
                         bool res = DoSomething(
                     }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "bool res = DoSomething(");
            Assert.That(completions, NotContains("new DateTime()"));
        }

        [Test]
        public async Task SuggestInterfaceImplementation_ConstructorParameter()
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

            var completions = await GetCompletionsAsync(Provider, source, "new Test1(");
            Assert.That(completions, Contains("new List<int>()"));
        }

        [Test]
        public async Task SuggestInterfaceImplementation_AfterNewKeyword()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test {
                    public void Method() {
                        IList<string> list = new 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = new ");
            Assert.That(completions, Contains("List<string>"));
        }

        [Test]
        public async Task SuggestInterfaceImplementation_UnimportedTypes()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        System.Collections.Generic.IList<string> list =  
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, Contains("new List<string>()", "System.Collections.Generic"));
        }

        [Test]
        public async Task DoNotSuggestGenericTypesIfConstraintNotSatisfied()
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

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, NotContains("new TContraints<string>()"));
        }

        [Test]
        public async Task DoNotSuggestNewNullableEnums()
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

            var completions = await GetCompletionsAsync(Provider, source, "enumField = ");
            Assert.That(completions, NotContains("new SomeEnum?()"));
        }

        [Test]
        public async Task SuggestGenericTypesIfTypeConstraintSatisfied()
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

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, Contains("new TContraints<SomeClassDerived>()"));
        }

        [Test]
        public async Task SuggestArrayInitialyzer()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        int[] =  
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, Contains("new [] {}"));
        }

        [Test]
        public async Task SuggestFactoryMethods()
        {
            const string source = @"
                using System;
                public class Test {
                    public async Task DoSmth()
                    {
                        TimeSpan ts = 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, Contains("TimeSpan.FromSeconds"));
        }

        [Test]
        public async Task DoNotSuggestStaticMethodsForBuiltInTypes()
        {
            const string source = @"
                using System;
                public class Test {
                    public void DoSmth()
                    {
                        string str = 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, NotContains("String.Concat"));
        }

        [Test]
        public async Task SuggestStaticProperties()
        {
            const string source = @"
                using System;
                public class Test {
                    public static bool DoSmth(Test testInstance)
                    {
                        DateTime dt = 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, Contains("DateTime.Now"));
        }

        [Test]
        public async Task SuggestListInitialyzer()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test {
                    public void Method() {
                        List<int> lst = 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, Contains("new List<int> {}"));
        }

        [TestCase("int", "Int32")]
        [TestCase("double", "Double")]
        [TestCase("string", "String")]
        [TestCase("IComparable", "Int32")]
        public async Task DoNotSuggestPrimitiveTypesConstructors(string shortName, string typeName)
        {
            var source = @"
                using System;

                public class Test {
                    public void Method() {" +
                       $"{shortName} v = " +
                    @"}
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, NotContains($"new {shortName}()"));
            Assert.That(completions, NotContains($"new {typeName}()"));
        }

        [Test]
        public async Task SuggestTrueFalseForBool()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                       bool b = 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = ");

            Assert.That(completions, Contains("true"));
            Assert.That(completions, Contains("false"));
        }

        [Test]
        public async Task DoNotSuggestAnythingIfNotApplicable()
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
                await Provider.ProvideCompletionsAsync(context);
                var completions = GetCompletions(context);

                Assert.That(completions, Is.Empty);
            }
        }

        [Test]
        public async Task DoNotSuggestAnythingForWrongMemberName()
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

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, Is.Empty);
        }

        [Test]
        public async Task DoNotSuggestToCreateNullableInstance()
        {
            const string source = @"
                using System;

                public class Test
                {
                    public void Method()
                    {
                        Guid? g = 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = ");

            Assert.That(completions, Contains("new Guid()") & NotContains("new Guid?()"));
        }

        [Test]
        public async Task DoNotSuggestAnythingIfVariableHasObjectType()
        {
            const string source = @"
                public class Test
                {
                    public void Method()
                    {
                        object o = 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, Is.Empty);
        }

        [Test]
        public async Task DoNotSuggestIfRequiresConversion()
        {
            const string source = @"
                public class ClassWithConversion
                {
                    public static implicit operator string(ClassWithConversion c) => """";
                }

                public class Test
                {
                    public void Method()
                    {
                        string s = 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, " = ");
            Assert.That(completions, NotContains("new ClassWithConversion()"));
        }

        [Test]
        public async Task SuggestOnReturn()
        {
            const string source = @"
                using System.Collections.Generic;

                public class Test {
                    public List<int> Method() {
                        return 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "return ");
            Assert.That(completions, Contains("new List<int>()"));
        }

        [Test]
        public async Task ShouldSuggestTaskGenericTypeForAsyncMethods()
        {
            const string source = @"
                using System.Collections.Generic;
                using System.Threading.Tasks;

                public class Test {
                    public async Task<List<int>> MethodAsync() {
                        return 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "return ");

            Assert.That(completions, Contains("new List<int>()"));
        }

        [Test]
        public async Task ShouldSuggestNestedTypesIfEnabled()
        {
            const string source = @"
                public class OuterClass
                {
                    public class InnerClass { }
                }

                public class Test {
                    public OuterClass.InnerClass Method() {
                        return 
                    }
                }";

            var provider = Provider_WithOptions(o => o.SuggestNestedTypes = true);
            var completions = await GetCompletionsAsync(provider, source, "return ");

            Assert.That(completions, Contains("new OuterClass.InnerClass()"));
        }

        [Test]
        public async Task ShouldSuggestNestedTypesAsGenericParameter()
        {
            const string source = @"
                using System.Collections.Generic;

                public class OuterClass
                {
                    public class InnerClass { }
                }

                public class Test {
                    public List<OuterClass.InnerClass> Method() {
                        return 
                    }
                }";

            var provider = Provider_WithOptions(o => o.SuggestNestedTypes = true);
            var completions = await GetCompletionsAsync(provider, source, "return ");

            Assert.That(completions, Contains("new List<OuterClass.InnerClass>()"));
        }

        [Test]
        public async Task ShouldAccountForUsingAliases_Generic()
        {
            const string source = @"
                using TestAlias = System.Collections.Generic.List<string>;

                public class Test {
                    public void Method() {
                        System.Collections.Generic.IList<string> list =  
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "list = ");
            Assert.That(completions, NotContains("new List<string>()", "System.Collections.Generic"));
            Assert.That(completions, Contains("new TestAlias()"));
        }

        [Test]
        public async Task ShouldAccountForUsingAliases_NonGeneric()
        {
            const string source = @"
                using TestAlias = System.Collections.ArrayList;

                public class Test {
                    public void Method() {
                        System.Collections.IList list =  
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "list = ");
            Assert.That(completions, NotContains("new ArrayList()", "System.Collections"));
            Assert.That(completions, Contains("new TestAlias()"));
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
