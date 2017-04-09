using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace IntelliSenseExtender.Editor
{
    public class NamespaceResolver
    {
        private DTE GetDTE()
        {
            return (DTE)Package.GetGlobalService(typeof(DTE));
        }

        private string GetNamespaceUsing(string nsName)
        {
            return $"using {nsName};" + Environment.NewLine;
        }

        /// <summary>
        /// Add namespace to current document
        /// </summary>
        public void AddNamespace(string nsName)
        {
            //TODO: use roslyn
            var dte = GetDTE();
            if (dte.ActiveDocument.Object() is TextDocument textDoc)
            {
                var textToInsert = GetNamespaceUsing(nsName);

                var startPoint = textDoc.StartPoint;
                var editPoint = textDoc.CreateEditPoint(startPoint);
                editPoint.Insert(textToInsert);
            }
        }
    }
}
