import 'dart:js' show JsObject;

class ChangedLineMap {
  final LineRange oldRange;
  final LineRange newRange;

  const ChangedLineMap(this.oldRange, this.newRange);

  ChangedLineMap.fromJsObject(JsObject j)
    : this(new LineRange.fromJsObject(j['Old']), new LineRange.fromJsObject(j['New']));
}

class CodeFix {
  final CodeFixId id;
  final String codeFixProviderName;
  final String codeActionTitle;
  final List<Diagnostic> diagnostics;
  final Document changedDocument;
  final List<List<TextPart>> newDocumentLines;
  final List<ChangedLineMap> changedLineMaps;

  const CodeFix(this.id, this.codeFixProviderName, this.codeActionTitle, this.diagnostics, this.changedDocument, this.newDocumentLines, this.changedLineMaps);
}

class CodeFixId {
  final int id;
  const CodeFixId(this.id);

  @override
  bool operator==(dynamic other) {
    return other is CodeFixId && id == other.id;
  }

  @override
  int get hashCode => id;
}

class Diagnostic {
  final DiagnosticId id;
  final Document document;
  final LinePosition start;
  final LinePosition end;
  final String diagnosticId;
  final DiagnosticSeverity severity;
  final String message;

  const Diagnostic(this.id, this.document, this.start, this.end, this.diagnosticId, this.severity, this.message);
}

class DiagnosticId {
  final int id;
  const DiagnosticId(this.id);

  @override
  bool operator==(dynamic other) {
    return other is DiagnosticId && id == other.id;
  }

  @override
  int get hashCode => id;
}

enum DiagnosticSeverity {
  hidden,
  info,
  warning,
  error
}

class Document {
  final DocumentId id;
  final Project project;
  final String name;
  final List<List<TextPart>> lines;

  const Document(this.id, this.project, this.name, this.lines);
}

class DocumentId {
  final int id;
  const DocumentId(this.id);

  @override
  bool operator==(dynamic other) {
    return other is DocumentId && id == other.id;
  }

  @override
  int get hashCode => id;
}

enum Language {
  csharp,
  visualBasic
}

class LinePosition {
  final int line;
  final int character;

  const LinePosition(this.line, this.character);

  LinePosition.fromJsObject(JsObject j)
    : this(j['Line'], j['Character']);
}

class LineRange {
  final int startLine;
  final int lineCount;

  const LineRange(this.startLine, this.lineCount);

  LineRange.fromJsObject(JsObject j)
    : this(j['StartLine'], j['LineCount']);
}

class Project {
  final ProjectId id;
  final String name;
  final Language language;
  final List<Telemetry> telemetryInfo;
  
  const Project(this.id, this.name, this.language, this.telemetryInfo);
}

class ProjectId {
  final int id;
  const ProjectId(this.id);

  @override
  bool operator==(dynamic other) {
    return other is ProjectId && id == other.id;
  }

  @override
  int get hashCode => id;
}

class Telemetry {
  final String diagnosticAnalyzerName;
  final int compilationStartActionsCount;
  final int compilationEndActionsCount;
  final int compilationActionsCount;
  final int syntaxTreeActionsCount;
  final int semanticModelActionsCount;
  final int symbolActionsCount;
  final int syntaxNodeActionsCount;
  final int codeBlockStartActionsCount;
  final int codeBlockEndActionsCount;
  final int codeBlockActionsCount;
  final int operationActionsCount;
  final int operationBlockStartActionsCount;
  final int operationBlockEndActionsCount;
  final int operationBlockActionsCount;
  final Duration executionTime;

  const Telemetry(this.diagnosticAnalyzerName, this.compilationStartActionsCount,
      this.compilationEndActionsCount, this.compilationActionsCount, this.syntaxTreeActionsCount,
      this.semanticModelActionsCount, this.symbolActionsCount, this.syntaxNodeActionsCount,
      this.codeBlockStartActionsCount, this.codeBlockEndActionsCount, this.codeBlockActionsCount,
      this.operationActionsCount, this.operationBlockStartActionsCount, this.operationBlockEndActionsCount,
      this.operationBlockActionsCount, this.executionTime);

  Telemetry.fromJsObject(JsObject j)
    : this(
      j['DiagnosticAnalyzerName'],
      j['CompilationStartActionsCount'],
      j['CompilationEndActionsCount'],
      j['CompilationActionsCount'],
      j['SyntaxTreeActionsCount'],
      j['SemanticModelActionsCount'],
      j['SymbolActionsCount'],
      j['SyntaxNodeActionsCount'],
      j['CodeBlockStartActionsCount'],
      j['CodeBlockEndActionsCount'],
      j['CodeBlockActionsCount'],
      j['OperationActionsCount'],
      j['OperationBlockStartActionsCount'],
      j['OperationBlockEndActionsCount'],
      j['OperationBlockActionsCount'],
      new Duration(microseconds:  j['ExecutionTimeInMicroseconds'])
    );
}

class TextPart {
  final TextPartType type;
  final String text;

  const TextPart(this.type, this.text);

  TextPart.fromJsObject(JsObject j)
    : this(TextPartType.values[j['Type']], j['Text']);
}

enum TextPartType {
  plain
}
