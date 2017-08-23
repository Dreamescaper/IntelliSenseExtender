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

		private string GetNamespaceUsing(string nsName) => $"using {nsName};" + Environment.NewLine;

		private string GetStaticUsing(string className) => $"using static {className};" + Environment.NewLine;


		/// <summary>
		/// Add namespace to current document
		/// </summary>
		public void AddNamespaceOrStatic(string namespaceOrClassName, bool isNamespace = true)
		{
			//TODO: use roslyn
			var dte = GetDTE();
			if (dte.ActiveDocument.Object() is TextDocument textDoc) {
				var textToInsert = isNamespace ? GetNamespaceUsing(namespaceOrClassName) : GetStaticUsing(namespaceOrClassName);

				var startPoint = textDoc.StartPoint;
				var editPoint = textDoc.CreateEditPoint(startPoint);
				editPoint.Insert(textToInsert);
			}
		}


	}
}
