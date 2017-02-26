namespace AnalyzerInsecta.OutputModels
{
    public class Diagnostic
    {
        public int? DocumentIndex { get; set; }
        public LinePosition Start { get; set; }
        public LinePosition End { get; set; }
        public string DiagnosticId { get; set; }
        public Microsoft.CodeAnalysis.DiagnosticSeverity Severity { get; set; }
        public string Message { get; set; }

        public Diagnostic() { }

        public Diagnostic(int? documentIndex, LinePosition start, LinePosition end, string diagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity severity, string message)
        {
            this.DocumentIndex = documentIndex;
            this.Start = start;
            this.End = end;
            this.DiagnosticId = diagnosticId;
            this.Severity = severity;
            this.Message = message;
        }
    }
}
