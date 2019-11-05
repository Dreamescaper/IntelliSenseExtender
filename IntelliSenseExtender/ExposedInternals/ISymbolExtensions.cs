using System;
using System.Linq;
using Microsoft.CodeAnalysis;

#nullable disable

namespace IntelliSenseExtender.ExposedInternals
{
    public static class ISymbolExtensions
    {
        private static readonly Func<ISymbol, int, bool> _isInaccessibleLocalMethod;

        static ISymbolExtensions()
        {
            var workspacesAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .First(a => a.GetName().Name == "Microsoft.CodeAnalysis.Workspaces");
            var type = workspacesAssembly.GetType("Microsoft.CodeAnalysis.Shared.Extensions.ISymbolExtensions");

            _isInaccessibleLocalMethod = (Func<ISymbol, int, bool>)type?.GetMethod(nameof(IsInaccessibleLocal))?.CreateDelegate(typeof(Func<ISymbol, int, bool>));
        }

        public static bool IsInaccessibleLocal(this ISymbol symbol, int position)
        {
            return _isInaccessibleLocalMethod(symbol, position);
        }
    }
}
