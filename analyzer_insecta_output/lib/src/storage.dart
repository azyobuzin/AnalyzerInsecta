import 'dart:js' show JsArray, JsObject;
import 'model.dart';

class AnalyzerInsectaStorage {
  final List<Diagnostic> _diagnostics;

  AnalyzerInsectaStorage(JsArray<JsObject> diagnosticsArray) :
    _diagnostics =
      new List.unmodifiable(
        new Iterable.generate(diagnosticsArray.length, (i) {
          final jo = diagnosticsArray[i];
          return new Diagnostic(
            new DiagnosticId(i),
            jo["ProjectId"],
            jo["DocumentId"],
            new LinePosition.fromJsObject(jo["Start"]),
            new LinePosition.fromJsObject(jo["End"]),
            jo["DiagnosticId"],
            DiagnosticSeverity.values[jo["Severity"]],
            jo["Message"]
          );
        })
      );

  Iterable<Diagnostic> get diagnostics => _diagnostics;

  Diagnostic findDiagnostic(DiagnosticId id) => _diagnostics[id.id];
}
