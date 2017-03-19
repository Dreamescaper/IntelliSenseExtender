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
        private static readonly Type _internalType;
        private static readonly MethodInfo _isTypeContextMethod;

        static SyntaxTreeExtensions()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var asm = assemblies.First(a => a.FullName.Contains("Microsoft.CodeAnalysis.CSharp.Workspaces"));

            _internalType = asm.GetType("Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery.SyntaxTreeExtensions");
            _isTypeContextMethod = _internalType.GetMethod("IsTypeContext");
        }


        public static bool IsTypeContext(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken, SemanticModel semanticModelOpt = null)
        {
            var result = _isTypeContextMethod.Invoke(null, new object[] { syntaxTree, position, cancellationToken, semanticModelOpt });
            return (bool)result;
        }
    }
}
