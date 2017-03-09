import 'dart:js' show JsArray, JsObject;
import 'model.dart';

class AnalyzerInsectaStorage {
  List<Project> _projects;
  List<Document> _documents;
  List<Diagnostic> _diagnostics;
  List<CodeFix> _codeFixes;

  AnalyzerInsectaStorage(JsObject j) {
    _projects = _mapToList(j['Projects'], (i, JsObject jo) =>
      new Project(
        new ProjectId(i),
        jo['Name'],
        Language.values[jo['Language']],
        new List.unmodifiable(jo['TelemetryInfo'].map((JsObject x) => new Telemetry.fromJsObject(x)))
      )
    );

    _documents = _mapToList(j['Documents'], (i, JsObject jo) =>
      new Document(
        new DocumentId(i),
        _projects[jo['ProjectIndex']],
        jo['Name'],
        _readLines(jo['Lines'])
      )
    );

    _diagnostics = _mapToList(j['Diagnostics'], (i, JsObject jo) {
      final int documentIndex = jo['DocumentIndex'];
      return new Diagnostic(
        new DiagnosticId(i),
        documentIndex == null ? null : _documents[documentIndex],
        new LinePosition.fromJsObject(jo['Start']),
        new LinePosition.fromJsObject(jo['End']),
        jo['DiagnosticId'],
        DiagnosticSeverity.values[jo['Severity']],
        jo['Message']
      );
    });

    _codeFixes = _mapToList(j['CodeFixes'], (i, JsObject jo) {
      final int changedDocumentIndex = jo['ChangedDocumentIndex'];
      return new CodeFix(
        new CodeFixId(i),
        jo['CodeFixProviderName'],
        jo['CodeActionTitle'],
        new List.unmodifiable(jo['DiagnosticIndexes'].map((int j) => _diagnostics[j])),
        changedDocumentIndex == null ? null : _documents[changedDocumentIndex],
        _readLines(jo['NewDocumentLines']),
        new List.unmodifiable(jo['ChangedLineMaps'].map((JsObject x) => new ChangedLineMap.fromJsObject(x)))
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
