import 'dart:js' show JsObject;

class Diagnostic {
  final DiagnosticId id;
  final String projectId; // TODO: 参照にしたいところ
  final String documentId;
  final LinePosition start;
  final LinePosition end;
  final String diagnosticId;
  final DiagnosticSeverity severity;
  final String message;

  const Diagnostic(this.id, this.projectId, this.documentId, this.start, this.end, this.diagnosticId, this.severity, this.message);
}

class DiagnosticId {
  final int id;
  const DiagnosticId(this.id);

  bool operator==(DiagnosticId other) {
    return id == other.id;
  }

  @override
  int get hashCode => id;
}

class LinePosition {
  final int line;
  final int character;

  const LinePosition(this.line, this.character);

  LinePosition.fromJsObject(JsObject j)
    : this((j["Line"] as num).toInt(), (j["Character"] as num).toInt());
}

enum DiagnosticSeverity {
  hidden,
  info,
  warning,
  error
}
