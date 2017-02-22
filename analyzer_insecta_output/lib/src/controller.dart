import 'model.dart';
import 'storage.dart';
import 'view_abstraction.dart';

class AnalyzerInsectaController {
  final AnalyzerInsectaView view;
  final AnalyzerInsectaStorage storage;

  AnalyzerInsectaController(this.view, this.storage);

  void start() {
    view.initialize();
  }
}
