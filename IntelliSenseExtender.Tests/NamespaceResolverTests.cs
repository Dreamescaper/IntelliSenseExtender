using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntelliSenseExtender.Editor;
using IntelliSenseExtender.Tests.CompletionProviders;
using NUnit.Framework;

namespace IntelliSenseExtender.Tests
{
    [TestFixture]
    public class NamespaceResolverTests : AbstractCompletionProviderTest
    {
        [Test]
        public async Task ShouldAddUsingAtSortedPlace()
        {
            const string source = @"
                using System;
                using System.Threading;

                namespace ns.something
                {
                    public class Test {/*here*/}
                }";

            var document = GetTestDocument(source);
            int position = source.IndexOf("/*here*/");
            var newDoc = await new NamespaceResolver().AddNamespaceImportAsync("System.Collections", document, position, CancellationToken.None);
            var newDocText = (await newDoc.GetTextAsync()).ToString();

            var usingIndexes = new[] {
                    "using System;",
                    "using System.Collections;",
                    "using System.Threading;" }
                .Select(u => newDocText.IndexOf(u))
                .ToArray();

            Assert.That(usingIndexes, Does.Not.Contain(-1), "Missing namespace!");
            Assert.That(usingIndexes, Is.Ordered);
        }

        [Test]
        public async Task ShouldAddUsingInsideNamespaceIfUsingsArePresent()
        {
            const string source = @"
                namespace ns.something
                {
                    using System;

                    public class Test {/*here*/}
                }";

            var document = GetTestDocument(source);
            int position = source.IndexOf("/*here*/");
            var newDoc = await new NamespaceResolver().AddNamespaceImportAsync("System.Collections", document, position, CancellationToken.None);
            var newDocText = (await newDoc.GetTextAsync()).ToString();

            Assert.That(newDocText, Does.Contain("using System.Collections;"));
            Assert.That(newDocText.IndexOf("using System.Collections;"),
                Is.GreaterThan(newDocText.IndexOf("namespace ns.something")));
        }

        [TestCase("A.B.C", "A.B.Csmth.D.E", "Csmth.D.E")]
        [TestCase("A.B.C", "A.B.C.smth.D.E", "smth.D.E")]
        [TestCase("A.B.C.D", "A.B.C.smth.D.E", "smth.D.E")]
        public async Task ShouldAddNamespaceRelativeToParentNamespace(string currentNamespace, string namespaceToImport, string expectedNamespace)
        {
            const string here = "/*here*/";
            string source = $@"
                namespace {currentNamespace}
                {{
                    using System;

                    public class Test {{{here}}}
                }}";

            var document = GetTestDocument(source);
            int position = source.IndexOf(here);
            var newDoc = await new NamespaceResolver().AddNamespaceImportAsync(namespaceToImport, document, position, CancellationToken.None);
            var newDocText = (await newDoc.GetTextAsync()).ToString();

            Assert.That(newDocText, Does.Not.Contain($"using {namespaceToImport};"));
            Assert.That(newDocText, Does.Contain($"using {expectedNamespace};"));
        }
    }
}
