using System.Linq;
using System.Threading.Tasks;
using IntelliSenseExtender.IntelliSense.Providers;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace IntelliSenseExtender.Tests.CompletionProviders
{
    public class LocalVariables : AbstractCompletionProviderTest
    {
        private readonly CompletionProvider Provider = new AggregateTypeCompletionProvider(
            Options_Default,
            new LocalsCompletionProvider());

        [Test]
        public async Task SuggestLocalVariablesForMethodsParameters()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        int i = 0;
                        IntMethod(
                    }

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, Contains("i"));
        }

        [Test]
        public async Task SuggestInlineDeclaredLocalVariables()
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

            var completions = await GetCompletionsAsync(Provider, source, "StrMethod(");
            Assert.That(completions, Contains("strVar"));
        }

        [Test]
        public async Task SuggestForeachVariable()
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

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, Contains("i"));
        }

        [Test]
        public async Task SuggestForVariable()
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

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, Contains("i"));
        }

        [Test]
        public async Task SuggestUsingVariable()
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

            var completions = await GetCompletionsAsync(Provider, source, "SrMethod(");
            Assert.That(completions, Contains("v"));
        }

        [Test]
        public async Task SuggestDeconstructedTuples()
        {
            const string source = @"
                public class Test {
                    public void Method()
                    {
                        var tuple = (1, 2);
                        var (d1, d2) = tuple;
                        IntMethod(
                    }

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, Contains("d1") & Contains("d2"));
        }

        [Test]
        public async Task SuggestLocalsForTupleMembers_FirstMember()
        {
            const string source = @"
                public class Test {
                    public (string r1, int r2) Method()
                    {
                        string v1 = ""a"";
                        int v2 = 2;

                        return (
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "return (");
            Assert.That(completions, Contains("v1") & NotContains("v2"));
        }

        [Test]
        public async Task SuggestLocalsForTupleMembers_SecondMember()
        {
            const string source = @"
                public class Test {
                    public (string r1, int r2) Method()
                    {
                        string v1 = ""a"";
                        int v2 = 2;

                        return (v1, 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "return (");
            Assert.That(completions, NotContains("v1") & Contains("v2"));
        }

        [Test]
        public async Task SuggestLocalsForTupleMembers_ThirdMember()
        {
            const string source = @"
                public class Test {
                    public (string r1, int r2, char r3) Method()
                    {
                        string v1 = ""a"";
                        int v2 = 2;
                        char v3 = 'c';

                        return (v1, v2, 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "return (");
            Assert.That(completions, NotContains("v1")
                & NotContains("v2") & Contains("v3"));
        }

        [Test]
        public async Task SuggestMethodParametersAsArguments()
        {
            const string source = @"
                public class Test {
                    public void Method(int i) {
                        IntMethod(
                    }

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, Contains("i"));
        }

        [Test]
        public async Task ResolveMethodsOverloadsBasedOnExistingArguments_1()
        {
            const string source = @"
                public class Test {
                    public void Method(int i, string s) {
                        TestMethod(i, 
                    }

                    public void TestMethod(string s1, string s2) { }
                    public void TestMethod(int i1, int i2) { }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "TestMethod(i, ");
            Assert.That(completions, Contains("i") & NotContains("s"));
        }

        [Test]
        public async Task ResolveMethodsOverloadsBasedOnExistingArguments_2()
        {
            const string source = @"
                public class Test {
                    public void Method(int i, string s) {
                        TestMethod(s, 
                    }

                    public void TestMethod(string s1, string s2) { }
                    public void TestMethod(int i1, int i2) { }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "TestMethod(s, ");
            Assert.That(completions, Contains("s") & NotContains("i"));
        }

        [Test]
        public async Task SuggestMethodParametersForPropertiesAssignment()
        {
            const string source = @"
                public class Test {
                    private int SomeProperty {get; set;} 

                    public void Method(int i) {
                        SomeProperty = 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "SomeProperty = ");
            Assert.That(completions, Contains("i"));
        }

        [Test]
        public async Task SuggestLambdaParameterOutsideMethod()
        {
            const string source = @"
                using System;
                public static class Test {
                    public static Func<string, string, bool> F = (a,b) => a.Contains(
                }";

            var completions = await GetCompletionsAsync(Provider, source, "a.Contains(");
            Assert.That(completions, Contains("b"));
        }

        [Test]
        public async Task SuggestLabmdaParameters()
        {
            const string source = @"
                using System;

                public class Test {
                    public void Method() {
                        Action<int> action = i => IntMethod(
                    }

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, Contains("i"));
        }

        [Test]
        public async Task SuggestProperties()
        {
            const string source = @"
                public class Test {
                    private int Prop => 0;

                    public void Method() {
                        IntMethod(
                    }

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, Contains("Prop"));
        }

        [Test]
        public async Task SuggestFields()
        {
            const string source = @"
                public class Test {
                    private int _field = 0;

                    public void Method() {
                        IntMethod(
                    }

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, Contains("_field"));
        }

        [Test]
        public async Task SuggestProtectedProperties()
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

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, Contains("ProtectedIntProperty"));
        }

        [Test]
        public async Task SuggestAssignableVariables()
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

            var completions = await GetCompletionsAsync(Provider, source, "AMethod(");
            Assert.That(completions, Contains("bVar"));
        }

        [Test]
        public async Task SuggestSetterValueParameter_Property()
        {
            const string source = @"
                public class TestClass
                {
                    public int Prop
                    {
                        get => 0;
                        set
                        {
                            Method(
                        }
                    }

                    public void Method(int i)
                    {
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "Method(");
            Assert.That(completions, Contains("value"));
        }

        [Test]
        public async Task SuggestSetterValueParameter_IndexedProperty()
        {
            const string source = @"
                public class TestClass
                {
                    public int this[string key]
                    {
                        set
                        {
                            Method(
                        }
                    }

                    public void Method(int i)
                    {
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "Method(");
            Assert.That(completions, Contains("value"));
        }

        [Test]
        public async Task SuggestIndexedPropertyIndexParameter()
        {
            const string source = @"
                public class TestClass
                {
                    public int this[string key]
                    {
                        set
                        {
                            Method(
                        }
                    }

                    public void Method(string str)
                    {
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "Method(");
            Assert.That(completions, Contains("key"));
        }

        [Test]
        public async Task SuggestForBinaryExpressions([Values("==", "!=", ">", "<", ">=", "<=")] string @operator)
        {
            string source = $@"
                public class TestClass
                {{
                    public void Method()
                    {{
                        int a = 0;
                        int b = 1;
                        if (a {@operator} 
                    }}
                }}";

            var completions = await GetCompletionsAsync(Provider, source, $"if (a {@operator} ");
            Assert.That(completions, Contains("b") & NotContains("a"));
        }

        [Test]
        public async Task DontSuggestNotSuitableVariables()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        string s = ""str"";
                        IntMethod(
                    }

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, NotContains("s"));
        }

        [Test]
        public async Task DontSuggestPrivatePropertiesFromBaseClass()
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

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, NotContains("PrivateIntProperty"));
        }

        [Test]
        public async Task DontSuggestLocalsOutOfScope()
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

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, NotContains("outOfScope"));
        }

        [Test]
        public async Task DontSuggestLocalsDefinedLater()
        {
            const string source = @"
                using System;

                public class A
                {
                    public void Method() {
                        IntMethod(
                        
                        int undefinedSoFar = 0;
                    }

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, NotContains("undefinedSoFar"));
        }

        [Test]
        public async Task DontSuggestForVariableOutOfFor()
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

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, NotContains("i"));
        }

        [Test]
        public async Task DontSuggestForeachVariableOutOfForeach()
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

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, NotContains("i"));
        }

        [Test]
        public async Task DontSuggestInstanceMembersInStaticMethod()
        {
            const string source = @"
                public class Test {
                    private int _field = 0;

                    public static void Method() {
                        IntMethod(
                    }

                    public static void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            Assert.That(completions, NotContains("_field"));
        }

        [Test]
        public async Task DontSuggestAnythingInArbitraryContext()
        {
            const string source = @"
                public class Test {
                    public void Method() {
                        int i = 0;
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
        public async Task DontSuggestSuggestAutoPropertiesBackingFields()
        {
            const string source = @"
                public class Test {
                    private int Prop {get; set;}

                    public void Method() {
                        IntMethod(
                    }

                    public void IntMethod(int v){ }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "IntMethod(");
            var completionNames = completions.Select(c => c.DisplayText).ToArray();

            Assert.That(completionNames, Has.All.Not.Contain("BackingField").IgnoreCase);
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
            var completionNames = completions.Select(c => c.DisplayText).ToArray();

            Assert.That(completionNames, Is.Empty);
        }

        [Test]
        public async Task DontSuggestSelfDuringAssignment()
        {
            const string source = @"
                public class Test {
                    private int Prop {get; set;}

                    public void Method() {
                        Prop = 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "Prop = ");
            Assert.That(completions, NotContains("Prop"));
        }

        [Test]
        public async Task SuggestCorrectArgumentType()
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

            var completions = await GetCompletionsAsync(Provider, source, "MultiMethod(_intField, ");

            Assert.That(completions, NotContains("_intField"));
            Assert.That(completions, NotContains("_boolField"));
            Assert.That(completions, Contains("_stringField"));
        }

        [Test]
        public async Task SuggestCorrectNamedArgumentType()
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

            var completions = await GetCompletionsAsync(Provider, source, "MultiMethod(boolVar:  ");

            Assert.That(completions, NotContains("_intField"));
            Assert.That(completions, NotContains("_stringField"));
            Assert.That(completions, Contains("_boolField"));
        }

        [Test]
        public async Task SuggestReturnValues()
        {
            const string source = @"
                public class Test {
                    public int Method()
                    {
                        int i = 0;
                        return 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "return ");

            Assert.That(completions, Contains("i"));
        }

        [Test]
        public async Task SuggestReturnValuesOfTaskGenericTypeForAsyncMethods()
        {
            const string source = @"                
                using System.Threading.Tasks;

                public class Test {
                    public async Task<int> MethodAsync()
                    {
                        int i = 0;
                        return 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "return ");

            Assert.That(completions, Contains("i"));
        }

        [Test]
        public async Task SuggestThisIfTypeApplicable()
        {
            const string source = @"                
                using System.Threading.Tasks;

                public class Test {
                    public Test Method()
                    {
                        return 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "return ");

            Assert.That(completions, Contains("this"));
        }

        [Test]
        public async Task DoNotSuggestThisIfTypeNotApplicable()
        {
            const string source = @"                
                using System.Threading.Tasks;

                public class Test {
                    public int Method()
                    {
                        return 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "return ");

            Assert.That(completions, NotContains("this"));
        }

        [Test]
        public async Task DoNotSuggestThisInStaticContext()
        {
            const string source = @"                
                using System.Threading.Tasks;

                public class Test {
                    public static Test StaticMethod()
                    {
                        return 
                    }
                }";

            var completions = await GetCompletionsAsync(Provider, source, "return ");

            Assert.That(completions, NotContains("this"));
        }

        [Test]
        public void DontTriggerInAttributeConstructor_FirstArgument()
        {
            // Due to strange default behavior with suggestion non-static members

            const string source = @"
                public class Test {
                    [Some(]
                    public static bool DoSmth(Test testInstance)
                    {
                    }
                }

                public SomeAttribute : System.Attribute
                {
                    public SomeAttribute(string v) { }
                }";

            bool triggerCompletion = Provider.ShouldTriggerCompletion(
                text: SourceText.From(source),
                caretPosition: source.IndexOf("[Some(") + 6,
                trigger: CompletionTrigger.CreateInsertionTrigger('('),
                options: null);
            Assert.That(!triggerCompletion);
        }

        [Test]
        public void DontTriggerInAttributeConstructor_SecondArgument()
        {
            // Due to strange default behavior with suggestion non-static members

            const string source = @"
                public class Test {
                    [Some(""0"", ]
                    public static bool DoSmth(Test testInstance)
                    {
                    }
                }

                public SomeAttribute : System.Attribute
                {
                    public SomeAttribute(string v1, string v2) { }
                }";

            bool triggerCompletion = Provider.ShouldTriggerCompletion(
                text: SourceText.From(source),
                caretPosition: source.IndexOf(", ") + 2,
                trigger: CompletionTrigger.CreateInsertionTrigger(' '),
                options: null);
            Assert.That(!triggerCompletion);
        }
    }
}
