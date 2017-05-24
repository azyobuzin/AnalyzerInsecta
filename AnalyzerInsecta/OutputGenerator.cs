using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using O = AnalyzerInsecta.OutputModels;
using R = Microsoft.CodeAnalysis;

namespace AnalyzerInsecta
{
    public sealed class OutputGenerator
    {
        private OutputGenerator() { }

        private O.Project[] _projects;
        private List<O.Document> _documents;
        private List<O.Diagnostic> _diagnostics;
        private List<O.CodeFix> _codeFixes;
        private Dictionary<R.Document, int> _documentIndexDictionary;
        private Dictionary<R.Diagnostic, int> _diagnosticIndexDictionary;

        public static Task<O.OutputModel> CreateModel(ProjectAnalysisResult[] projectAnalysisResults)
        {
            return new OutputGenerator().CreateModelCore(projectAnalysisResults);
        }

        public async Task<O.OutputModel> CreateModelCore(ProjectAnalysisResult[] projectAnalysisResults)
        {
            this._projects = new O.Project[projectAnalysisResults.Length];
            this._documents = new List<O.Document>();
            this._diagnostics = new List<O.Diagnostic>();
            this._codeFixes = new List<O.CodeFix>();
            this._documentIndexDictionary = new Dictionary<R.Document, int>();
            this._diagnosticIndexDictionary = new Dictionary<R.Diagnostic, int>();

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

                this._projects[i] = new O.Project(
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
                            result.Diagnostics
                        ).ConfigureAwait(false);
                    }

                    this._diagnosticIndexDictionary.Add(diagnostic, this._diagnostics.Count);

                    var lineSpan = diagnostic.Location.GetLineSpan();

                    this._diagnostics.Add(new O.Diagnostic(
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
                        documentIndex = this._documentIndexDictionary[changedDocument.OldDocument];
                        newDocumentLines = await CreateDocumentLines(changedDocument.NewDocument, ImmutableArray<R.Diagnostic>.Empty).ConfigureAwait(false);
                        changedLineMaps = await CreateChangedLineMaps(changedDocument.OldDocument, changedDocument.NewDocument).ConfigureAwait(false);
                    }

                    this._codeFixes.Add(new O.CodeFix(
                        codeFix.CodeFixProviderName,
                        codeFix.CodeActionTitle,
                        codeFix.Diagnostics.ToArray(x => this._diagnosticIndexDictionary[x]),
                        documentIndex,
                        newDocumentLines,
                        changedLineMaps
                    ));
                }
            }

            return new O.OutputModel(
                this._projects,
                this._documents.ToArray(),
                this._diagnostics.ToArray(),
                this._codeFixes.ToArray()
            );
        }

        private async Task<int> GetDocumentIndexOrAdd(int projectIndex, R.Document document, ImmutableArray<R.Diagnostic> diagnostics)
        {
            if (!this._documentIndexDictionary.TryGetValue(document, out var index))
            {
                index = this._documents.Count;
                this._documents.Add(await CreateDocument(projectIndex, document, diagnostics).ConfigureAwait(false));
                this._documentIndexDictionary.Add(document, index);
            }

            return index;
        }

        private static async Task<O.Document> CreateDocument(int projectIndex, R.Document document, ImmutableArray<R.Diagnostic> diagnostics)
        {
            return new O.Document(
                projectIndex,
                string.Join("/", document.Folders.Concat(new[] { document.Name })),
                await CreateDocumentLines(document, diagnostics).ConfigureAwait(false)
            );
        }

        private static async Task<O.TextPart[][]> CreateDocumentLines(R.Document document, ImmutableArray<R.Diagnostic> diagnostics)
        {
            var tree = await document.GetSyntaxTreeAsync().ConfigureAwait(false);
            var text = await document.GetTextAsync().ConfigureAwait(false);

            return text.Lines
                .Select(x =>
                {
                    var parts = new LinkedList<WorkingTextPart>();
                    // TODO: シンタックスハイライト
                    parts.AddFirst(new WorkingTextPart(O.TextPartType.Plain, x.Span, null));

                    foreach (var diagnostic in diagnostics)
                    {
                        if (diagnostic.Location.SourceTree != tree) continue;

                        var diagSpan = diagnostic.Location.SourceSpan;
                        if (!x.Span.IntersectsWith(diagSpan)) continue;

                        var node = parts.First;
                        while (node != null)
                        {
                            var part = node.Value;

                            if ((!part.Severity.HasValue || part.Severity.Value < diagnostic.Severity)
                                && part.Span.Intersection(diagSpan) is R.Text.TextSpan intersection)
                            {
                                if (intersection.Start > part.Span.Start)
                                {
                                    parts.AddBefore(
                                        node,
                                        new WorkingTextPart(
                                            part.Type,
                                            new R.Text.TextSpan(part.Span.Start, intersection.Start - part.Span.Start),
                                            part.Severity
                                        )
                                    );
                                }

                                node.Value = new WorkingTextPart(part.Type, intersection, diagnostic.Severity);

                                if (intersection.End < part.Span.End)
                                {
                                    node = parts.AddAfter(
                                        node,
                                        new WorkingTextPart(
                                            part.Type,
                                            new R.Text.TextSpan(intersection.End, part.Span.End - intersection.End),
                                            part.Severity
                                        )
                                    );
                                }
                            }

                            node = node.Next;
                        }
                    }

                    return parts
                        .Select(y => new O.TextPart(y.Type, text.ToString(y.Span), y.Severity))
                        .ToArray();
                })
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

        private struct WorkingTextPart
        {
            public O.TextPartType Type { get; }
            public R.Text.TextSpan Span { get; }
            public R.DiagnosticSeverity? Severity { get; }

            public WorkingTextPart(O.TextPartType type, R.Text.TextSpan span, R.DiagnosticSeverity? severity)
            {
                this.Type = type;
                this.Span = span;
                this.Severity = severity;
            }
        }
    }
}
