namespace AnalyzerInsecta.OutputModels
{
    public class LineRange
    {
        public int StartLine { get; set; }
        public int LineCount { get; set; }

        public LineRange() { }

        public LineRange(int startLine, int lineCount)
        {
            this.StartLine = startLine;
            this.LineCount = lineCount;
        }
    }
}
