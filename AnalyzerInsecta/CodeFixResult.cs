using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AnalyzerInsecta
{
    public class CodeFixResult
    {
        public string CodeFixProviderName { get; }
        public string CodeActionTitle { get; }
        public DocumentSpan DiagnosticSpan { get; }
        public bool IsApplyChangeOperation { get; }
        public ImmutableArray<ChangedDocument> Changes { get; }

        public CodeFixResult(string codeFixProviderName, string codeActionTitle, DocumentSpan diagnosticSpan, bool isApplyChangeOperation, ImmutableArray<ChangedDocument> changes)
        {
            this.CodeFixProviderName = codeFixProviderName;
            this.CodeActionTitle = codeActionTitle;
            this.DiagnosticSpan = diagnosticSpan;
            this.IsApplyChangeOperation = isApplyChangeOperation;
            this.Changes = changes;
        }
    }

    public struct DocumentSpan
    {
        public Document Document { get; }
        public TextSpan TextSpan { get; }

        public DocumentSpan(Document document, TextSpan textSpan)
        {
            this.Document = document;
            this.TextSpan = textSpan;
        }
    }

    public struct ChangedDocument
    {
        public Document OldDocument { get; }
        public Document NewDocument { get; }

        public ChangedDocument(Document oldDocument, Document newDocument)
        {
            this.OldDocument = oldDocument;
            this.NewDocument = newDocument;
        }
    }
}
