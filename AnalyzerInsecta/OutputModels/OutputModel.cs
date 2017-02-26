namespace AnalyzerInsecta.OutputModels
{
    public class OutputModel
    {
        public Project[] Projects { get; set; }
        public Document[] Documents { get; set; }
        public Diagnostic[] Diagnostics { get; set; }
        public CodeFix[] CodeFixes { get; set; }

        public OutputModel() { }

        public OutputModel(Project[] projects, Document[] documents, Diagnostic[] diagnostics, CodeFix[] codeFixes)
        {
            this.Projects = projects;
            this.Documents = documents;
            this.Diagnostics = diagnostics;
            this.CodeFixes = codeFixes;
        }
    }
}
