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
    }
}
