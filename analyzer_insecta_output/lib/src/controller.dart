import 'dart:async';
import 'model.dart';
import 'storage.dart';

class AnalyzerInsectaController {
  final AnalyzerInsectaStorage storage;
  final _onRequestOpeningDocumentStreamController = new StreamController<OpenDocumentRequest>.broadcast(); // ignore: close_sinks

  AnalyzerInsectaController(this.storage);

  Stream<OpenDocumentRequest> get onRequestOpeningDocument => _onRequestOpeningDocumentStreamController.stream;

  void diagnosticClicked(DiagnosticId id) {
    final diagnostic = storage.getDiagnostic(id);
    _onRequestOpeningDocumentStreamController.add(new OpenDocumentRequest(
      diagnostic.document,
      diagnostic.start.line
    ));
  }
}

class OpenDocumentRequest {
  final Document document;
  final int line;

  const OpenDocumentRequest(this.document, this.line);
}
