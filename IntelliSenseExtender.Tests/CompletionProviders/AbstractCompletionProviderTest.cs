using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using IntelliSenseExtender.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;

namespace IntelliSenseExtender.Tests.CompletionProviders
{
    public abstract class AbstractCompletionProviderTest
    {
        private static MetadataReference Mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static MetadataReference SystemCore = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);

        #region Options

        public static OptionsProvider Options_Default => new OptionsProvider(
            new Options.Options
            {
                EnableExtensionMethodsSuggestions = true,
                EnableTypesSuggestions = true,
                FilterOutObsoleteSymbols = true,
                SortCompletionsAfterImported = true,
                SuggestOnObjectCreation = true,
                UserCodeOnlySuggestions = false
            });

        public static OptionsProvider Options_ExtensionMethodsOnly => new OptionsProvider(
            new Options.Options
            {
                EnableExtensionMethodsSuggestions = true,
                EnableTypesSuggestions = false,
                FilterOutObsoleteSymbols = true,
                SortCompletionsAfterImported = true,
                UserCodeOnlySuggestions = false
            });

        public static OptionsProvider Options_TypesOnly => new OptionsProvider(
            new Options.Options
            {
                EnableExtensionMethodsSuggestions = false,
                EnableTypesSuggestions = true,
                FilterOutObsoleteSymbols = true,
                SortCompletionsAfterImported = true,
                UserCodeOnlySuggestions = false
            });

        #endregion

        public static Document GetTestDocument(string source)
        {
            return GetTestDocument(source, new string[] { });
        }

        public static Document GetTestDocument(string source, params string[] additionalFiles)
        {
            ProjectId projectId = ProjectId.CreateNewId();
            DocumentId documentId = DocumentId.CreateNewId(projectId);

            var solution = new AdhocWorkspace().CurrentSolution
                .AddProject(projectId, "MyProject", "MyProject", LanguageNames.CSharp)
                .AddMetadataReference(projectId, Mscorlib)
                .AddMetadataReference(projectId, SystemCore)
                .AddDocument(documentId, "MyFile.cs", source);

            for (int i = 0; i < additionalFiles.Length; i++)
            {
                solution = solution.AddDocument(DocumentId.CreateNewId(projectId), $"{i}.cs", additionalFiles[i]);
            }

            var document = solution.GetDocument(documentId);

            return document;
        }

        public static CompletionContext GetContext(Document document, CompletionProvider provider, int position)
        {
            var context = new CompletionContext(provider, document, position,
                new TextSpan(), CompletionTrigger.Invoke, document.Project.Solution.Workspace.Options,
                default(CancellationToken));
            return context;
        }

        public static IReadOnlyList<CompletionItem> GetCompletions(CompletionContext context)
        {
            var property = context.GetType().GetProperty("Items", BindingFlags.Instance | BindingFlags.NonPublic);
            return (IReadOnlyList<CompletionItem>)property.GetValue(context);
        }

        public static IReadOnlyList<CompletionItem> GetCompletions(CompletionProvider provider, string source, string additinalFile, string searchText)
        {
            int position = source.IndexOf(searchText) + searchText.Length;

            var document = GetTestDocument(source, additinalFile);
            var completionContext = GetContext(document, provider, position);
            provider.ProvideCompletionsAsync(completionContext).Wait();
            var completions = GetCompletions(completionContext);
            return completions;
        }

        public static IReadOnlyList<CompletionItem> GetCompletions(CompletionProvider provider, string source, string searchText)
        {
            int position = source.IndexOf(searchText) + searchText.Length;

            var document = GetTestDocument(source);
            var completionContext = GetContext(document, provider, position);
            provider.ProvideCompletionsAsync(completionContext).Wait();
            var completions = GetCompletions(completionContext);
            return completions;
        }

        public class OptionsProvider : IOptionsProvider
        {
            private readonly Options.Options _options;

            public OptionsProvider(Options.Options options)
            {
                _options = options;
            }

            public Options.Options GetOptions()
            {
                return _options;
            }
        }
    }
}
