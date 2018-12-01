using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntelliSenseExtender.Extensions
{
    public static class SyntaxTreeExtensions
    {
        public static IReadOnlyList<string> GetImportedNamespaces(this SyntaxTree tree)
        {
            if (!(tree.GetRoot() is CompilationUnitSyntax compilationUnitSyntax))
                return new string[] { };

            var childNodes = compilationUnitSyntax.ChildNodes().ToArray();

            var namespaces = childNodes
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name.ToString()).ToList();

            var currentNamespaces = childNodes
                .OfType<NamespaceDeclarationSyntax>()
                .Select(nsSyntax => nsSyntax.Name.ToString());

            namespaces.AddRange(currentNamespaces.SelectMany(GetParentNamespaces));

            return namespaces;
        }

        private static IReadOnlyList<string> GetParentNamespaces(string nsName)
        {
            const char dot = '.';

            var parentNamespaces = nsName
                .Select((c, index) => c == dot ? index : -1)
                .Where(i => i != -1)
                .Select(dotIndex => nsName.Substring(0, dotIndex))
                .ToList();

            parentNamespaces.Add(nsName);

            return parentNamespaces;
        }
    }
}
