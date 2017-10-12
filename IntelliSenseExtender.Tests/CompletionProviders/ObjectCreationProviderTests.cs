using IntelliSenseExtender.IntelliSense.Providers;
using NUnit.Framework;
using System.Linq;

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
            Assert.That(completionsNames, Does.Contain("List<string>()"));
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
    }
}
