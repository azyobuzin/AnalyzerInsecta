﻿using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AnalyzerInsecta
{
    public class CodeFixResult
    {
        public string CodeFixProviderName { get; }
        public string CodeActionTitle { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public DocumentSpan DiagnosticSpan { get; }
        public bool IsSupportedOperation { get; }
        public ImmutableArray<ChangedDocument> Changes { get; }

        public CodeFixResult(string codeFixProviderName, string codeActionTitle, ImmutableArray<Diagnostic> diagnostics, DocumentSpan diagnosticSpan, bool isSupportedOperation, ImmutableArray<ChangedDocument> changes)
        {
            this.CodeFixProviderName = codeFixProviderName;
            this.CodeActionTitle = codeActionTitle;
            this.Diagnostics = diagnostics;
            this.DiagnosticSpan = diagnosticSpan;
            this.IsSupportedOperation = isSupportedOperation;
            this.Changes = changes;
        }
    }

    public struct DocumentSpan : IEquatable<DocumentSpan>
    {
        public Document Document { get; }
        public TextSpan TextSpan { get; }

        public DocumentSpan(Document document, TextSpan textSpan)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            this.Document = document;
            this.TextSpan = textSpan;
        }

        public bool Equals(DocumentSpan other)
        {
            return Equals(this.Document, other.Document)
                && this.TextSpan.Equals(other.TextSpan);
        }

        public override bool Equals(object obj)
        {
            return obj is DocumentSpan && this.Equals((DocumentSpan)obj);
        }

        public override int GetHashCode()
        {
            return unchecked(
                (this.Document.GetHashCode() * 397)
                ^ this.TextSpan.GetHashCode()
            );
        }
    }

    public struct ChangedDocument
    {
        public Document OldDocument { get; }
        public Document NewDocument { get; }

        public ChangedDocument(Document oldDocument, Document newDocument)
        {
            if (oldDocument == null && newDocument == null)
                throw new ArgumentException();

            this.OldDocument = oldDocument;
            this.NewDocument = newDocument;
        }
    }
}
