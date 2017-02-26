namespace AnalyzerInsecta.OutputModels
{
    public class TextPart
    {
        public TextPartType Type { get; set; }
        public string Text { get; set; }

        public TextPart() { }

        public TextPart(TextPartType type, string text)
        {
            this.Type = type;
            this.Text = text;
        }
    }
}
