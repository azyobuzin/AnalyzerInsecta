namespace AnalyzerInsecta.OutputModels
{
    public class CodeFix
    {
        public string CodeFixProviderName { get; set; }
        public string CodeActionTitle { get; set; }
        public int[] DiagnosticIndexes { get; set; }
        public int? ChangedDocumentIndex { get; set; }
        public TextPart[][] NewDocumentLines { get; set; }
        public ChangedLineMap[] ChangedLineMaps { get; set; }

        public CodeFix() { }

        public CodeFix(string codeFixProviderName, string codeActionTitle, int[] diagnosticIndexes, int? changedDocumentIndex, TextPart[][] newDocumentLines, ChangedLineMap[] changedLineMaps)
        {
            this.CodeFixProviderName = codeFixProviderName;
            this.CodeActionTitle = codeActionTitle;
            this.DiagnosticIndexes = diagnosticIndexes;
            this.ChangedDocumentIndex = changedDocumentIndex;
            this.NewDocumentLines = newDocumentLines;
            this.ChangedLineMaps = changedLineMaps;
        }
    }
}
