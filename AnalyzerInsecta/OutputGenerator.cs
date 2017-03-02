using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
                        documentIndex = await GetDocumentIndexOrAdd(
                            i,
                            result.Project.GetDocument(syntaxTree),
                            documents,
                            documentIndexDictionary
                        ).ConfigureAwait(false);
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
                        newDocumentLines = await CreateDocumentLines(changedDocument.NewDocument).ConfigureAwait(false);
                        changedLineMaps = await CreateChangedLineMaps(changedDocument.OldDocument, changedDocument.NewDocument).ConfigureAwait(false);
                    }

                    codeFixes.Add(new O.CodeFix(
                        codeFix.CodeFixProviderName,
                        codeFix.CodeActionTitle,
                        codeFix.Diagnostics.ToArray(x => diagnosticIndexDictionary[x]),
                        documentIndex,
                        newDocumentLines,
                        changedLineMaps
                    ));
                }
            }

            return new O.OutputModel(
                projects,
                documents.ToArray(),
                diagnostics.ToArray(),
                codeFixes.ToArray()
            );
        }

        private static async Task<int> GetDocumentIndexOrAdd(int projectIndex, R.Document document, List<O.Document> documents, Dictionary<R.Document, int> documentIndexDictionary)
        {
            int index;
            if (!documentIndexDictionary.TryGetValue(document, out index))
            {
                index = documents.Count;
                documents.Add(await CreateDocument(projectIndex, document).ConfigureAwait(false));
                documentIndexDictionary.Add(document, index);
            }

            return index;
        }

        private static async Task<O.Document> CreateDocument(int projectIndex, R.Document document)
        {
            return new O.Document(
                projectIndex,
                string.Join("/", document.Folders.Concat(new[] { document.Name })),
                await CreateDocumentLines(document).ConfigureAwait(false)
            );
        }

        private static async Task<O.TextPart[][]> CreateDocumentLines(R.Document document)
        {
            // TODO: シンタックスハイライト
            return (await document.GetTextAsync().ConfigureAwait(false))
                .Lines
                .Select(x => new[] { new O.TextPart(O.TextPartType.Plain, x.ToString()) })
                .ToArray();
        }

        private static async Task<O.ChangedLineMap[]> CreateChangedLineMaps(R.Document oldDocument, R.Document newDocument)
        {
            var changes = await newDocument.GetTextChangesAsync(oldDocument).ConfigureAwait(false);
            var oldText = await oldDocument.GetTextAsync().ConfigureAwait(false);

            var lineChanges = new List<LineChange>();

            foreach (var change in changes)
            {
                var startLine = oldText.Lines.IndexOf(change.Span.Start);
                var oldLineCount = oldText.ToString(change.Span).Count(c => c == '\n') + 1;
                var endLine = startLine + oldLineCount;
                var additionalLineCount = change.NewText.Count(c => c == '\n') + 1 - oldLineCount;

                // 近いものがあるならマージしていく
                var isMerged = false;
                for (var i = 0; i < lineChanges.Count; i++)
                {
                    var lc = lineChanges[i];

                    // change
                    // lc
                    // みたいな配置になっている場合
                    var b1 = startLine <= lc.StartLine && startLine + oldLineCount >= lc.StartLine;

                    // lc
                    // change
                    var b2 = lc.StartLine <= startLine && lc.StartLine + lc.OldLineCount >= startLine;

                    isMerged = b1 || b2;
                    if (isMerged)
                    {
                        var newStartLine = Math.Min(startLine, lc.StartLine);
                        lineChanges[i] = new LineChange(
                            newStartLine,
                            Math.Max(endLine, lc.StartLine + lc.OldLineCount) - newStartLine,
                            lc.AdditionalLineCount + additionalLineCount
                        );

                        break;
                    }
                }

                if (!isMerged) lineChanges.Add(new LineChange(startLine, oldLineCount, additionalLineCount));
            }

            // StartLine 順に並べて、変更後の行数を反映しながら結果を詰めていく
            lineChanges.Sort((x, y) => x.StartLine.CompareTo(y.StartLine));

            var changedLineMaps = new O.ChangedLineMap[lineChanges.Count];
            var additional = 0;

            for (var i = 0; i < changedLineMaps.Length; i++)
            {
                var lc = lineChanges[i];
                changedLineMaps[i] = new O.ChangedLineMap(
                    new O.LineRange(lc.StartLine, lc.OldLineCount),
                    new O.LineRange(lc.StartLine + additional, lc.OldLineCount + lc.AdditionalLineCount)
                );

                additional += lc.AdditionalLineCount;
            }

            return changedLineMaps;
        }

        private struct LineChange
        {
            public int StartLine { get; }
            public int OldLineCount { get; }
            public int AdditionalLineCount { get; }

            public LineChange(int startLine, int oldLineCount, int additionalCount)
            {
                this.StartLine = startLine;
                this.OldLineCount = oldLineCount;
                this.AdditionalLineCount = additionalCount;
            }
        }
    }
}
