using System;
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
                return Array.Empty<string>();

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
            var parentNamespaces = new List<string>();

            for (int i = 0; i < nsName.Length; i++)
            {
                if (nsName[i] == '.')
                    parentNamespaces.Add(nsName.Substring(0, i));
            }

            parentNamespaces.Add(nsName);

            return parentNamespaces;
        }
    }
}
