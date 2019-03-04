using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using IntelliSenseExtender.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace IntelliSenseExtender.Tests.CompletionProviders
{
    public abstract class AbstractCompletionProviderTest
    {
        private static readonly MetadataReference Mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        private static readonly MetadataReference SystemCore = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);

        #region Options

        private static Options.Options GetDefaultOptions() =>
            new Options.Options
            {
                FilterOutObsoleteSymbols = true,
                SortCompletionsAfterImported = true,
                SuggestTypesOnObjectCreation = true,
                AddParethesisForNewSuggestions = true,
                SuggestFactoryMethodsOnObjectCreation = true,
                SuggestLocalVariablesFirst = true,
                InvokeIntelliSenseAutomatically = true
            };

        public static OptionsProvider Options_Default => new OptionsProvider(GetDefaultOptions());
        public static OptionsProvider Options_With(Action<Options.Options> modifyOptions)
        {
            var options = GetDefaultOptions();
            modifyOptions(options);
            return new OptionsProvider(options);
        }

        #endregion

        public static Document GetTestDocument(string source)
        {
            return GetTestDocument(source, Array.Empty<string>());
        }

        public static Document GetTestDocument(string source, params string[] additionalFiles)
        {
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var solution = new AdhocWorkspace().CurrentSolution
                .AddProject(projectId, "MyProject", "MyProject", LanguageNames.CSharp)
                .AddMetadataReference(projectId, Mscorlib)
                .AddMetadataReference(projectId, SystemCore)
                .AddDocument(documentId, "MyFile.cs", source);

            for (int i = 0; i < additionalFiles.Length; i++)
            {
                solution = solution.AddDocument(DocumentId.CreateNewId(projectId), $"{i}.cs", additionalFiles[i]);
            }

            return solution.GetDocument(documentId);
        }

        public static CompletionContext GetContext(Document document, CompletionProvider provider, int position)
        {
            return new CompletionContext(provider, document, position,
                new TextSpan(), CompletionTrigger.Invoke, document.Project.Solution.Workspace.Options,
                default);
        }

        public static IReadOnlyList<CompletionItem> GetCompletions(CompletionContext context)
        {
            var property = context.GetType().GetProperty("Items", BindingFlags.Instance | BindingFlags.NonPublic);
            return (IReadOnlyList<CompletionItem>)property.GetValue(context);
        }

        public static async Task<IReadOnlyList<CompletionItem>> GetCompletionsAsync(CompletionProvider provider, string source, string additinalFile, string searchText)
        {
            int position = GetPosition(source, searchText);

            var document = GetTestDocument(source, additinalFile);
            var completionContext = GetContext(document, provider, position);
            await provider.ProvideCompletionsAsync(completionContext);
            return GetCompletions(completionContext);
        }

        public static async Task<IReadOnlyList<CompletionItem>> GetCompletionsAsync(CompletionProvider provider, string source, string searchText)
        {
            int position = GetPosition(source, searchText);

            var document = GetTestDocument(source);
            var completionContext = GetContext(document, provider, position);
            await provider.ProvideCompletionsAsync(completionContext);
            return GetCompletions(completionContext);
        }

        public static async Task<IReadOnlyList<CompletionItem>> GetCompletionsAsync(CompletionProvider provider, Document document, string searchText)
        {
            var sourceText = await document.GetTextAsync();
            int position = GetPosition(sourceText.ToString(), searchText);

            var completionContext = GetContext(document, provider, position);
            await provider.ProvideCompletionsAsync(completionContext);
            return GetCompletions(completionContext);
        }

        public static int GetPosition(string source, string searchText)
        {
            var beforePosision = source.IndexOf(searchText);

            if (beforePosision == -1)
                throw new Exception("Search text not found!");

            return beforePosision + searchText.Length;
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

        #region Assertion Constraints

        public static bool Matches(CompletionItem item, string itemText, string @namespace)
        {
            var expectedItemText = itemText;

            if (@namespace != null)
                expectedItemText += $"  ({@namespace})";

            return item.DisplayText == expectedItemText;
        }

        public static Constraint Contains(string itemText, string @namespace = null)
        {
            return Has.Some.EqualTo(itemText).Using<CompletionItem, string>(
                (item, _) => Matches(item, itemText, @namespace));
        }

        public static Constraint NotContains(string itemText, string @namespace = null)
        {
            return new NotConstraint(Contains(itemText, @namespace));
        }

        #endregion Assertion Constraints
    }
}
