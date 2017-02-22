using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;

namespace AnalyzerInsecta
{
    internal class ProjectAnalysisResult
    {
        public Project Project { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> AnalyzerTelemetryInfo { get; }
        public ImmutableArray<CodeFixResult> CodeFixes { get; }

        public ProjectAnalysisResult(Project project, ImmutableArray<Diagnostic> diagnostics,
            ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> analyzerTelemetryInfo,
            ImmutableArray<CodeFixResult> codeFixes)
        {
            this.Project = project;
            this.Diagnostics = diagnostics;
            this.AnalyzerTelemetryInfo = analyzerTelemetryInfo;
            this.CodeFixes = codeFixes;
        }
    }
}
