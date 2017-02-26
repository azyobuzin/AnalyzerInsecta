namespace AnalyzerInsecta.OutputModels
{
    public class Document
    {
        public int ProjectIndex { get; set; }
        public string Name { get; set; }
        public TextPart[][] Lines { get; set; }

        public Document() { }

        public Document(int projectIndex, string name, TextPart[][] lines)
        {
            this.ProjectIndex = projectIndex;
            this.Name = name;
            this.Lines = lines;
        }
    }
}
