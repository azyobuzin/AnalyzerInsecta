import 'dart:html' as html;
import 'dart:js' as js;
import 'dart:math';
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
    _dockManager = new ds.DockManager(html.document.getElementById('dock-manager') as html.DivElement);
    _dockManager.initialize();

    html.window.onResize.listen(_onResized);
    _onResized(null);

    final documentManagerNode = _dockManager.context.model.documentManagerNode;
    final diagnosticsPanel = new ErrorListPanel(_controller, _dockManager);
    final bottomNode = _dockManager.dockDown(documentManagerNode, diagnosticsPanel, 0.3);
    _dockManager.dockFill(bottomNode, new TelemetryPanel(_controller, _dockManager));
    (bottomNode.parent.container as ds.FillDockContainer).tabHost.setActiveTab(diagnosticsPanel);

    _controller.onRequestOpeningDocument.listen(_onRequestOpeningDocument);
  }

  void _onResized(html.Event e) {
    _dockManager.resize(html.window.innerWidth, html.window.innerHeight);
  }

  void _onRequestOpeningDocument(OpenDocumentRequest request) {
    final documentManagerNode = _dockManager.context.model.documentManagerNode;

    // TODO: 他のノードのタブを調べる
    for (var page in (documentManagerNode.container as ds.DocumentManagerContainer).tabHost.pages) {
      final container = page.container;
      if (container is DocumentPanel && container.document.id == request.document.id) {
        container.jumpToLine(request.line);
        return;
      }
    }

    final panel = new DocumentPanel(_dockManager, request.document);
    _dockManager.dockFill(documentManagerNode, panel);
    panel.jumpToLine(request.line);
  }
}

class UnclosablePanelContainer extends ds.PanelContainer {
  UnclosablePanelContainer(html.Element elementContent, ds.DockManager dockManager, [String title = 'Panel'])
    : super(elementContent, dockManager, title) {
    elementButtonClose.remove();

    // TODO: タブになったときに閉じられないようにする
  }
}

class ErrorListPanel extends UnclosablePanelContainer {
  final AnalyzerInsectaController _controller;

  ErrorListPanel(this._controller, ds.DockManager dockManager)
    : super(new html.DivElement(), dockManager, 'Error List') {
    final table = new html.TableElement();
    table.classes.add('errorlist-table');

    // Header
    {
      table.createTHead().addRow()
        ..addCell().text = 'Code'
        ..addCell().text = 'Description'
        ..addCell().text = 'Project'
        ..addCell().text = 'File'
        ..addCell().text = 'Line';
    }

    final tbody = table.createTBody();

    _controller.storage.diagnostics.forEach((diagnostic) {
      tbody.addRow()
        ..onClick.listen((e) => _controller.diagnosticClicked(diagnostic.id))
        ..addCell().text = '${_getSeverityIcon(diagnostic.severity)} ${diagnostic.diagnosticId}'
        ..addCell().text = diagnostic.message
        ..addCell().text = diagnostic.document.project.name
        ..addCell().text = diagnostic.document.name
        ..addCell().text = (diagnostic.start.line + 1).toString();
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
      default:
        throw new ArgumentError.value(severity, "severity");
    }
  }
}

class TelemetryPanel extends UnclosablePanelContainer {
  final AnalyzerInsectaController _controller;

  TelemetryPanel(this._controller, ds.DockManager dockManager)
    : super(new html.DivElement(), dockManager, 'Telemetry Info') {
    final content = new html.DivElement();
    content.classes.add('telemetry-content');

    _controller.storage.projects.forEach((project) {
      final header = new html.HeadingElement.h3();
      header.text = project.name;
      content.append(header);

      project.telemetryInfo.forEach((telemetry) {
        final table = new html.TableElement();
        table.createCaption().text = telemetry.diagnosticAnalyzerName;

        final mirror = mirrors.reflect(telemetry);
        mirror.type.declarations.forEach((s, vm) {
          if (vm is mirrors.VariableMirror) {
            if (s == #diagnosticAnalyzerName || vm.isStatic) return;

            table.addRow()
              ..addCell().text = mirrors.MirrorSystem.getName(s)
              ..addCell().text = mirror.getField(s).reflectee.toString();
          }
        });

        content.append(table);
      });
    });

    elementContent.classes.add('telemetry-container');
    elementContent.append(content);
  }
}

class DocumentPanel extends ds.PanelContainer {
  final Document document;
  html.DivElement _lineNumberContainer;
  html.DivElement _documentContent;
  final _lineNumbers = new List<html.DivElement>();
  final _contentLines = new List<html.DivElement>();

  DocumentPanel(ds.DockManager dockManager, this.document)
    : super(new html.DivElement(), dockManager, document.name) {
    _lineNumberContainer = new html.DivElement();
    _lineNumberContainer.classes.add('linenumber-container');

    for (var i = 1; i <= document.lines.length; i++) {
      final e = new html.DivElement()
        ..classes.add('linenumber')
        ..text = i.toString();
      _lineNumberContainer.append(e);
      _lineNumbers.add(e);
    }

    _documentContent = new html.DivElement();
    _documentContent.classes.add('document-content');

    document.lines.forEach((line) {
      final lineElement = new html.DivElement();
      lineElement.classes.add('documentline');
      line.forEach((part) => lineElement.append(_textPartToSpan(part)));
      _documentContent.append(lineElement);
      _contentLines.add(lineElement);
    });

    elementContent.classes.add('document-container');
    elementContent.append(_lineNumberContainer);
    elementContent.append(_documentContent);

    _documentContent.onScroll.listen((e) {
      _lineNumberContainer.scrollTop = _documentContent.scrollTop;
    });
  }

  static html.SpanElement _textPartToSpan(TextPart textPart) {
    final span = new html.SpanElement();

    switch (textPart.type) {
      case TextPartType.plain:
        break;
    }

    span.text = textPart.text;
    return span;
  }

  @override
  void resize(int width, int height) {
    super.resize(width, height);

    _documentContent.style.left = '${_lineNumberContainer.offsetWidth}px';
    _documentContent.style.width = '${elementContent.clientWidth - _lineNumberContainer.offsetWidth}px';
  }

  void jumpToLine(int line) {
    for (var i = 0; i < document.lines.length; i++) {
      if (i == line) {
        _lineNumbers[i].classes.add('line-highlighted');
        _contentLines[i].classes.add('line-highlighted');
      } else {
        _lineNumbers[i].classes.remove('line-highlighted');
        _contentLines[i].classes.remove('line-highlighted');
      }
    }

    _documentContent.scrollTop = max(0, _contentLines[line].offsetTop - 30);
  }
}
