import 'dart:html';
import 'dart:js' as js;
import 'package:dock_spawn/dock_spawn.dart' as ds;
import 'package:analyzer_insecta_output/analyzer_insecta_output.dart';

void main() {
  final controller = new AnalyzerInsectaController(
    new AnalyzerInsectaStorage(js.context["analyzerInsectaData"])
  );
  final view = new ViewImpl(controller);
  view.start();
}

class ViewImpl {
  final AnalyzerInsectaController _controller;
  ds.DockManager _dockManager;

  ViewImpl(this._controller);

  void start() {
    _dockManager = new ds.DockManager(document.getElementById('dock-manager'));
    _dockManager.initialize();

    window.onResize.listen(_onResized);
    _onResized(null);

    final documentNode = _dockManager.context.model.documentManagerNode;
    final diagnosticsPanel = new DiagnosticsPanel(_controller, _dockManager);
    final bottomNode = _dockManager.dockDown(documentNode, diagnosticsPanel, 0.3);
    _dockManager.dockFill(bottomNode, new TelemetryPanel(_controller, _dockManager));
    (bottomNode.parent.container as ds.FillDockContainer).tabHost.setActiveTab(diagnosticsPanel);
  }

  void _onResized(Event e) {
    _dockManager.resize(window.innerWidth, window.innerHeight);
  }
}

class UnclosablePanelContainer extends ds.PanelContainer {
  UnclosablePanelContainer(Element elementContent, ds.DockManager dockManager, [String title = "Panel"])
    : super(elementContent, dockManager, title) {
    elementButtonClose.remove();

    // TODO: タブになったときに閉じられないようにする
  }
}

class DiagnosticsPanel extends UnclosablePanelContainer {
  final AnalyzerInsectaController _controller;

  DiagnosticsPanel(this._controller, ds.DockManager dockManager)
    : super(_createElementContent(), dockManager, "Error List");

  static Element _createElementContent() {
    final container = new DivElement();
    container.appendText("Hello1");

    return container;
  }
}

class TelemetryPanel extends UnclosablePanelContainer {
  final AnalyzerInsectaController _controller;

  TelemetryPanel(this._controller, ds.DockManager dockManager)
      : super(_createElementContent(), dockManager, "Telemetry Info");

  static Element _createElementContent() {
    final container = new DivElement();
    container.appendText("Hello2");

    return container;
  }
}
