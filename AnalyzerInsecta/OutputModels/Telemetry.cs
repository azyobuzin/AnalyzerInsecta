namespace AnalyzerInsecta.OutputModels
{
    public class Telemetry
    {
        public string DiagnosticAnalyzerName { get; set; }
        public int CompilationStartActionsCount { get; set; }
        public int CompilationEndActionsCount { get; set; }
        public int CompilationActionsCount { get; set; }
        public int SyntaxTreeActionsCount { get; set; }
        public int SemanticModelActionsCount { get; set; }
        public int SymbolActionsCount { get; set; }
        public int SyntaxNodeActionsCount { get; set; }
        public int CodeBlockStartActionsCount { get; set; }
        public int CodeBlockEndActionsCount { get; set; }
        public int CodeBlockActionsCount { get; set; }
        public int OperationActionsCount { get; set; }
        public int OperationBlockStartActionsCount { get; set; }
        public int OperationBlockEndActionsCount { get; set; }
        public int OperationBlockActionsCount { get; set; }
        public long ExecutionTimeInMicroseconds { get; set; }

        public Telemetry() { }

        public Telemetry(string diagnosticAnalyzerName, Microsoft.CodeAnalysis.Diagnostics.Telemetry.AnalyzerTelemetryInfo analyzerTelemetryInfo)
        {
            this.DiagnosticAnalyzerName = diagnosticAnalyzerName;
            this.CompilationStartActionsCount = analyzerTelemetryInfo.CompilationStartActionsCount;
            this.CompilationEndActionsCount = analyzerTelemetryInfo.CompilationEndActionsCount;
            this.CompilationActionsCount = analyzerTelemetryInfo.CompilationActionsCount;
            this.SyntaxTreeActionsCount = analyzerTelemetryInfo.SyntaxTreeActionsCount;
            this.SemanticModelActionsCount = analyzerTelemetryInfo.SemanticModelActionsCount;
            this.SymbolActionsCount = analyzerTelemetryInfo.SymbolActionsCount;
            this.SyntaxNodeActionsCount = analyzerTelemetryInfo.SyntaxNodeActionsCount;
            this.CodeBlockStartActionsCount = analyzerTelemetryInfo.CodeBlockStartActionsCount;
            this.CodeBlockEndActionsCount = analyzerTelemetryInfo.CodeBlockEndActionsCount;
            this.CodeBlockActionsCount = analyzerTelemetryInfo.CodeBlockActionsCount;
            this.OperationActionsCount = analyzerTelemetryInfo.OperationActionsCount;
            this.OperationBlockStartActionsCount = analyzerTelemetryInfo.OperationBlockStartActionsCount;
            this.OperationBlockEndActionsCount = analyzerTelemetryInfo.OperationBlockEndActionsCount;
            this.OperationBlockActionsCount = analyzerTelemetryInfo.OperationBlockActionsCount;
            this.ExecutionTimeInMicroseconds = analyzerTelemetryInfo.ExecutionTime.Ticks / 10;
        }
    }
}
