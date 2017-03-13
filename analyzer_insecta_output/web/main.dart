import 'dart:html';
import 'dart:js' as js;
@mirrors.MirrorsUsed(targets: 'Telemetry')
import 'dart:mirrors' as mirrors;
import 'package:dock_spawn/dock_spawn.dart' as ds;
import 'package:analyzer_insecta_output/analyzer_insecta_output.dart';

void main() {
  final controller = new AnalyzerInsectaController(
    new AnalyzerInsectaStorage(js.context['analyzerInsectaData'] as js.JsObject)
  );
  final view = new ViewImpl(controller);
  view.start();
}

class ViewImpl {
  final AnalyzerInsectaController _controller;
  ds.DockManager _dockManager;

  ViewImpl(this._controller);

  void start() {
    _dockManager = new ds.DockManager(document.getElementById('dock-manager') as DivElement);
    _dockManager.initialize();

    window.onResize.listen(_onResized);
    _onResized(null);

    final documentNode = _dockManager.context.model.documentManagerNode;
    final diagnosticsPanel = new ErrorListPanel(_controller, _dockManager);
    final bottomNode = _dockManager.dockDown(documentNode, diagnosticsPanel, 0.3);
    _dockManager.dockFill(bottomNode, new TelemetryPanel(_controller, _dockManager));
    (bottomNode.parent.container as ds.FillDockContainer).tabHost.setActiveTab(diagnosticsPanel);
  }

  void _onResized(Event e) {
    _dockManager.resize(window.innerWidth, window.innerHeight);
  }
}

class UnclosablePanelContainer extends ds.PanelContainer {
  UnclosablePanelContainer(Element elementContent, ds.DockManager dockManager, [String title = 'Panel'])
    : super(elementContent, dockManager, title) {
    elementButtonClose.remove();

    // TODO: タブになったときに閉じられないようにする
  }
}

class ErrorListPanel extends UnclosablePanelContainer {
  final AnalyzerInsectaController _controller;

  ErrorListPanel(this._controller, ds.DockManager dockManager)
    : super(new DivElement(), dockManager, 'Error List') {
    final table = new TableElement();
    table.classes.add('errorlist-table');

    // Header
    {
      final headRow = table.createTHead().addRow();
      headRow.addCell().text = 'Code';
      headRow.addCell().text = 'Description';
      headRow.addCell().text = 'Project';
      headRow.addCell().text = 'File';
      headRow.addCell().text = 'Line';
    }

    final tbody = table.createTBody();

    _controller.storage.diagnostics.forEach((diagnostic) {
      final row = tbody.addRow();
      row.addCell().text = '${_getSeverityIcon(diagnostic.severity)} ${diagnostic.diagnosticId}';
      row.addCell().text = diagnostic.message;
      row.addCell().text = diagnostic.document.project.name;
      row.addCell().text = diagnostic.document.name;
      row.addCell().text = (diagnostic.start.line + 1).toString();
    });

    elementContent.classes.add('errorlist-container');
    elementContent.append(table);
  }

  static String _getSeverityIcon(DiagnosticSeverity severity) {
    switch (severity) {
      case DiagnosticSeverity.hidden:
        return '⯑';
      case DiagnosticSeverity.info:
        return 'ℹ';
      case DiagnosticSeverity.warning:
        return '⚠';
      case DiagnosticSeverity.error:
        return '❌';
    }
  }
}

class TelemetryPanel extends UnclosablePanelContainer {
  final AnalyzerInsectaController _controller;

  TelemetryPanel(this._controller, ds.DockManager dockManager)
    : super(new DivElement(), dockManager, 'Telemetry Info') {
    final content = new DivElement();
    content.classes.add('telemetry-content');

    _controller.storage.projects.forEach((project) {
      final header = new HeadingElement.h3();
      header.text = project.name;
      content.append(header);

      project.telemetryInfo.forEach((telemetry) {
        final table = new TableElement();
        table.createCaption().text = telemetry.diagnosticAnalyzerName;

        final mirror = mirrors.reflect(telemetry);
        mirror.type.declarations.forEach((s, vm) {
          if (vm is mirrors.VariableMirror) {
            if (s == #diagnosticAnalyzerName || vm.isStatic) return;

            final row = table.addRow();
            row.addCell().text = mirrors.MirrorSystem.getName(s);
            row.addCell().text = mirror.getField(s).reflectee.toString();
          }
        });

        content.append(table);
      });
    });

    elementContent.classes.add('telemetry-container');
    elementContent.append(content);
  }
}
