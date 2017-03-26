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
            if (tree.GetRoot() is CompilationUnitSyntax compilationUnitSyntax)
            {
                var childNodes = compilationUnitSyntax.ChildNodes().ToArray();

                var namespaces = childNodes
                    .OfType<UsingDirectiveSyntax>()
                    .Select(u => u.Name.ToString()).ToList();

                var currentNamespaces = childNodes
                    .OfType<NamespaceDeclarationSyntax>()
                    .Select(nsSyntax => nsSyntax.Name.ToString());

                namespaces.AddRange(currentNamespaces);

                return namespaces;
            }
            else
            {
                return new string[] { };
            }
        }
    }
}
