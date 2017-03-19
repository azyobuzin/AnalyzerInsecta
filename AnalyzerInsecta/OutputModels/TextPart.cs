namespace AnalyzerInsecta.OutputModels
{
    public class TextPart
    {
        public TextPartType Type { get; set; }
        public string Text { get; set; }
        public Microsoft.CodeAnalysis.DiagnosticSeverity? Severity { get; set; }

        public TextPart() { }

        public TextPart(TextPartType type, string text, Microsoft.CodeAnalysis.DiagnosticSeverity? severity)
        {
            this.Type = type;
            this.Text = text;
            this.Severity = severity;
        }
    }
}
