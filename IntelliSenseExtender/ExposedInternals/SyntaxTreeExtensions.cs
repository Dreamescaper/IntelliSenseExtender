using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace IntelliSenseExtender.ExposedInternals
{
    /// <summary>
    /// Exposing some methods from internal class
    /// <see cref="Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery.SyntaxTreeExtensions"/>
    /// </summary>
    public static class SyntaxTreeExtensions
    {
        private static readonly Type _contextQueryInternalType;
        private static readonly MethodInfo _isTypeContextMethod;
        private static readonly MethodInfo _isAttributeNameContextMethod;

        private static readonly Type _sharedInternalType;
        private static readonly MethodInfo _findTokenOnLeftOfPositionMethod;

        static SyntaxTreeExtensions()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var cSharpWorkspacesAssembly = assemblies.First(a => a.FullName.Contains("Microsoft.CodeAnalysis.CSharp.Workspaces"));

            _contextQueryInternalType = cSharpWorkspacesAssembly.GetType("Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery.SyntaxTreeExtensions");
            _isTypeContextMethod = _contextQueryInternalType.GetMethod("IsTypeContext");
            _isAttributeNameContextMethod = _contextQueryInternalType.GetMethod("IsAttributeNameContext");

            var workspacesAssembly = assemblies.First(a => a.FullName.Contains("Microsoft.CodeAnalysis.Workspaces"));
            _sharedInternalType = workspacesAssembly.GetType("Microsoft.CodeAnalysis.Shared.Extensions.SyntaxTreeExtensions");
            _findTokenOnLeftOfPositionMethod = _sharedInternalType.GetMethod("FindTokenOnLeftOfPosition");
        }

        public static bool IsTypeContext(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken, SemanticModel semanticModelOpt = null)
        {
            var result = _isTypeContextMethod.Invoke(null, new object[] { syntaxTree, position, cancellationToken, semanticModelOpt });
            return (bool)result;
        }

        public static bool IsAttributeNameContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            var result = _isAttributeNameContextMethod.Invoke(null, new object[] { syntaxTree, position, cancellationToken });
            return (bool)result;
        }

        public static SyntaxToken FindTokenOnLeftOfPosition(
            this SyntaxTree syntaxTree,
            int position,
            CancellationToken cancellationToken,
            bool includeSkipped = true,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            var result = _findTokenOnLeftOfPositionMethod.Invoke(null, new object[]
                { syntaxTree, position, cancellationToken, includeSkipped, includeDirectives, includeDocumentationComments });
            return (SyntaxToken)result;
        }
    }
}
