import 'dart:js' show JsArray, JsObject;
import 'model.dart';

class AnalyzerInsectaStorage {
  List<Project> _projects;
  List<Document> _documents;
  List<Diagnostic> _diagnostics;
  List<CodeFix> _codeFixes;

  AnalyzerInsectaStorage(JsObject j) {
    _projects = _mapToList(j['Projects'] as JsArray<JsObject>, (i, JsObject jo) =>
      new Project(
        new ProjectId(i),
        jo['Name'] as String,
        Language.values[jo['Language'] as int],
        new List.unmodifiable((jo['TelemetryInfo'] as JsArray<JsObject>).map((x) => new Telemetry.fromJsObject(x)))
      )
    );

    _documents = _mapToList(j['Documents'] as JsArray<JsObject>, (i, JsObject jo) =>
      new Document(
        new DocumentId(i),
        _projects[jo['ProjectIndex'] as int],
        jo['Name'] as String,
        _readLines(jo['Lines'] as JsArray<JsArray<JsObject>>)
      )
    );

    _diagnostics = _mapToList(j['Diagnostics'] as JsArray<JsObject>, (i, JsObject jo) {
      final documentIndex = jo['DocumentIndex'] as int;
      return new Diagnostic(
        new DiagnosticId(i),
        documentIndex == null ? null : _documents[documentIndex],
        new LinePosition.fromJsObject(jo['Start'] as JsObject),
        new LinePosition.fromJsObject(jo['End'] as JsObject),
        jo['DiagnosticId'] as String,
        DiagnosticSeverity.values[jo['Severity'] as int],
        jo['Message'] as String
      );
    });

    _codeFixes = _mapToList(j['CodeFixes'] as JsArray<JsObject>, (i, JsObject jo) {
      final changedDocumentIndex = jo['ChangedDocumentIndex'] as int;
      return new CodeFix(
        new CodeFixId(i),
        jo['CodeFixProviderName'] as String,
        jo['CodeActionTitle'] as String,
        new List.unmodifiable((jo['DiagnosticIndexes'] as JsArray<int>).map((j) => _diagnostics[j])),
        changedDocumentIndex == null ? null : _documents[changedDocumentIndex],
        _readLines(jo['NewDocumentLines'] as JsArray<JsArray<JsObject>>),
        new List.unmodifiable((jo['ChangedLineMaps'] as JsArray<JsObject>).map((x) => new ChangedLineMap.fromJsObject(x)))
      );
    });
  }

  Iterable<Project> get projects => _projects;
  Project getProject(ProjectId id) => _projects[id.id];

  Iterable<Document> get documents => _documents;
  Document getDocument(DocumentId id) => _documents[id.id];

  Iterable<Diagnostic> get diagnostics => _diagnostics;
  Diagnostic getDiagnostic(DiagnosticId id) => _diagnostics[id.id];

  Iterable<CodeFix> get codeFixes => _codeFixes;
  CodeFix getCodeFix(CodeFixId id) => _codeFixes[id.id];
}

List<List<TextPart>> _readLines(JsArray<JsArray<JsObject>> source) {
  return new List.unmodifiable(source.map((x) =>
    new List<TextPart>.unmodifiable(x.map((y) => new TextPart.fromJsObject(y)))
  ));
}

List<E> _mapToList<S, E>(List<S> source, E f(int index, S sourceElement)) {
  return new List.unmodifiable(
    new Iterable.generate(source.length, (i) => f(i, source[i]))
  );
}
