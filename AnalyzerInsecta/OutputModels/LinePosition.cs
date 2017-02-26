namespace AnalyzerInsecta.OutputModels
{
    public class LinePosition
    {
        public int Line { get; set; }
        public int Character { get; set; }

        public LinePosition() { }

        public LinePosition(Microsoft.CodeAnalysis.Text.LinePosition source)
        {
            this.Line = source.Line;
            this.Character = source.Character;
        }
    }
}
