using System.Threading;
using IntelliSenseExtender.Editor;
using NUnit.Framework;

namespace IntelliSenseExtender.Tests.CompletionProviders
{
    [TestFixture]
    public class UtilsTests : AbstractCompletionProviderTest
    {
        [Test]
        public void NamespaceResolver_AddUsing()
        {
            // Basically verify that all that reflection crap is not broken.

            const string source = @"
                namespace ns.something
                {
                    public class Test {}
                }";

            var document = GetTestDocument(source);
            var newDoc = new NamespaceResolver().AddNamespaceImport("System", document, CancellationToken.None).Result;
            var newDocText = newDoc.GetTextAsync().Result.ToString();

            Assert.That(newDocText, Does.Contain("using System;"));
        }

        [Test]
        public void NamespaceResolver_ShouldAddUsingInsideNamespaceIfUsingsArePresent()
        {
            const string source = @"
                namespace ns.something
                {
                    using System;

                    public class Test {}
                }";

            var document = GetTestDocument(source);
            var newDoc = new NamespaceResolver().AddNamespaceImport("System.Collections", document, CancellationToken.None).Result;
            var newDocText = newDoc.GetTextAsync().Result.ToString();

            Assert.That(newDocText, Does.Contain("using System.Collections;"));
            Assert.That(newDocText.IndexOf("using System.Collections;"),
                Is.GreaterThan(newDocText.IndexOf("namespace ns.something")));
        }
    }
}
