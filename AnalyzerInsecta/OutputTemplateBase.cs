using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace AnalyzerInsecta
{
    public abstract class OutputTemplateBase
    {
        public TextWriter Writer { get; set; }

        public OutputTemplateModel Model { get; set; }

        public abstract Task ExecuteAsync();

        protected void WriteLiteral(string literal)
        {
            this.Writer.Write(literal);
        }

        protected void Write(object obj)
        {
            var raw = obj as RawHtml;
            if (raw != null)
            {
                this.Writer.Write(raw.Content);
            }
            else
            {
                WebUtility.HtmlEncode(obj.ToString(), this.Writer);
            }
        }

        protected class RawHtml
        {
            public string Content { get; }

            public RawHtml(string content)
            {
                this.Content = content;
            }
        }
    }
}
