import 'dart:html';
import 'dart:js' as js;
import 'package:dock_spawn/dock_spawn.dart' as ds;
import 'package:analyzer_insecta_output/analyzer_insecta_output.dart';

void main() {
  final controller = new AnalyzerInsectaController(
    new ViewImpl(),
    new AnalyzerInsectaStorage(js.context["diagnostics"])
  );

  controller.start();
}

class ViewImpl extends AnalyzerInsectaView {
  ds.DockManager _dockManager;

  @override
  void initialize() {
    _dockManager = new ds.DockManager(document.getElementById('dock-manager'));
    _dockManager.initialize();
    window.onResize.listen((e) => _dockManager.resize(window.innerWidth, window.innerHeight));
  }
}
