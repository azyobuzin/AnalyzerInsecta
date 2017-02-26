namespace AnalyzerInsecta.OutputModels
{
    public class ChangedLineMap
    {
        public LineRange Old { get; set; }
        public LineRange New { get; set; }

        public ChangedLineMap() { }

        public ChangedLineMap(LineRange old, LineRange @new)
        {
            this.Old = old;
            this.New = @new;
        }
    }
}
