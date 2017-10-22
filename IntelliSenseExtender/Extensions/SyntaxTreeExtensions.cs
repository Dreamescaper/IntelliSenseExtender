using System.Collections.Generic;
using System.Linq;
using System.Threading;
using IntelliSenseExtender.ExposedInternals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IntelliSenseExtender.Extensions
{
	public static class SyntaxTreeExtensions
	{
		public static IReadOnlyList<string> GetImportedNamespaces(this SyntaxTree tree)
		{
			if (tree.GetRoot() is CompilationUnitSyntax compilationUnitSyntax) {
				var childNodes = compilationUnitSyntax.ChildNodes().ToArray();

				var namespaces = childNodes
						.OfType<UsingDirectiveSyntax>()
						.Where(u => u.StaticKeyword.Value == null)
						.Select(u => u.Name.ToString()).ToList();

				var currentNamespaces = childNodes
						.OfType<NamespaceDeclarationSyntax>()
						.Select(nsSyntax => nsSyntax.Name.ToString());

				namespaces.AddRange(currentNamespaces);
				namespaces.AddRange(currentNamespaces.SelectMany(GetParentNamespaces));

				return namespaces;
			}
			else {
				return new string[] { };
			}
		}

		public static IReadOnlyList<string> GetImportedStatics(this SyntaxTree tree)
		{
			if (tree.GetRoot() is CompilationUnitSyntax compilationUnitSyntax) {
				var childNodes = compilationUnitSyntax.ChildNodes().ToArray();

				return childNodes
						.OfType<UsingDirectiveSyntax>()
						.Where(u => u.StaticKeyword.Value != null)
						.Select(u => u.Name.ToString()).ToList();
			}
			else {
				return new string[] { };
			}
		}

		public static bool IsMemberAccessContext(this SyntaxTree syntaxTree, int position, out ExpressionSyntax accessedExpressionSyntax, CancellationToken cancellationToken)
		{
			accessedExpressionSyntax = null;

			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			if (token.Kind() == SyntaxKind.DotToken
					&& token.Parent is MemberAccessExpressionSyntax memberAccessNode) {
				accessedExpressionSyntax = memberAccessNode.Expression;
			}
			return accessedExpressionSyntax != null;
		}

		private static IReadOnlyList<string> GetParentNamespaces(string nsName)
		{
			var splittedNs = nsName.Split('.');

			var parentNamespaces = new List<string>();
			for (int i = 1; i < splittedNs.Length; i++) {
				var parentNs = string.Join(".", splittedNs.Take(i));
				parentNamespaces.Add(parentNs);
			}

			return parentNamespaces;
		}
	}
}
