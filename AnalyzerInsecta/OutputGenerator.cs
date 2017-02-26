using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using O = AnalyzerInsecta.OutputModels;
using R = Microsoft.CodeAnalysis;

namespace AnalyzerInsecta
{
    public static class OutputGenerator
    {
        public static async Task<O.OutputModel> CreateModel(ProjectAnalysisResult[] projectAnalysisResults)
        {
            var projects = new O.Project[projectAnalysisResults.Length];
            var documents = new List<O.Document>();
            var diagnostics = new List<O.Diagnostic>();
            var codeFixes = new List<O.CodeFix>();
            var documentIndexDictionary = new Dictionary<R.Document, int>();
            var diagnosticIndexDictionary = new Dictionary<R.Diagnostic, int>();

            for (var i = 0; i < projectAnalysisResults.Length; i++)
            {
                var result = projectAnalysisResults[i];

                O.Language lang;
                switch (result.Project.Language)
                {
                    case R.LanguageNames.CSharp:
                        lang = O.Language.CSharp;
                        break;
                    case R.LanguageNames.VisualBasic:
                        lang = O.Language.VisualBasic;
                        break;
                    default:
                        throw new NotSupportedException($"'{result.Project.Language}' is not a supported language.");
                }

                projects[i] = new O.Project(
                    result.Project.Name,
                    lang,
                    result.AnalyzerTelemetryInfo
                        .Select(x => new O.Telemetry(x.Key.GetType().Name, x.Value))
                        .ToArray()
                );

                foreach (var diagnostic in result.Diagnostics)
                {
                    int? documentIndex = null;
                    var syntaxTree = diagnostic.Location.SourceTree;
                    if (syntaxTree != null)
                    {
                        documentIndex = GetDocumentIndexOrAdd(
                            i,
                            result.Project.GetDocument(syntaxTree),
                            documents,
                            documentIndexDictionary
                        );
                    }

                    diagnosticIndexDictionary.Add(diagnostic, diagnostics.Count);

                    var lineSpan = diagnostic.Location.GetLineSpan();

                    diagnostics.Add(new O.Diagnostic(
                        documentIndex,
                        new O.LinePosition(lineSpan.StartLinePosition),
                        new O.LinePosition(lineSpan.EndLinePosition),
                        diagnostic.Id,
                        diagnostic.Severity,
                        diagnostic.GetMessage(CultureInfo.CurrentCulture)
                    ));
                }

                foreach (var codeFix in result.CodeFixes)
                {
                    int? documentIndex = null;
                    O.TextPart[][] newDocumentLines = null;
                    O.ChangedLineMap[] changedLineMaps = null;
                    if (codeFix.ChangedDocument.HasValue)
                    {
                        var changedDocument = codeFix.ChangedDocument.Value;
                        documentIndex = documentIndexDictionary[changedDocument.OldDocument];
                        newDocumentLines = CreateDocumentLines(changedDocument.NewDocument);

                        var changes = (await changedDocument.NewDocument.GetSyntaxTreeAsync().ConfigureAwait(false))
                            .GetChanges(await changedDocument.OldDocument.GetSyntaxTreeAsync().ConfigureAwait(false));

                        // TODO
                    }

                    // TODO: Add to codeFixes
                }
            }
        }

        private static int GetDocumentIndexOrAdd(int projectIndex, R.Document document, List<O.Document> documents, Dictionary<R.Document, int> documentIndexDictionary)
        {
            int index;
            if (!documentIndexDictionary.TryGetValue(document, out index))
            {
                index = documents.Count;
                documents.Add(CreateDocument(projectIndex, document));
                documentIndexDictionary.Add(document, index);
            }

            return index;
        }

        private static O.Document CreateDocument(int projectIndex, R.Document document)
        {
            throw new NotImplementedException();
        }

        private static O.TextPart[][] CreateDocumentLines(R.Document document)
        {
            throw new NotImplementedException();
        }
    }
}
