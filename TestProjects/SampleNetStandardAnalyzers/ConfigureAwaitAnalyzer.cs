using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SampleNetStandardAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConfigureAwaitAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            "Sample0002", "Call ConfigureAwait",
            "The SynchronizationContext will be captured implicitly if you do not call ConfigureAwait",
            "Sample", DiagnosticSeverity.Info, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(CompilationStartAction);
        }

        private static void CompilationStartAction(CompilationStartAnalysisContext context)
        {
            var taskSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var futureSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

            if (taskSymbol == null && futureSymbol == null) return;

            context.RegisterSyntaxNodeAction(
                x => AwaitExpressionSyntaxAction(x, taskSymbol, futureSymbol),
                SyntaxKind.AwaitExpression
            );
        }

        private static void AwaitExpressionSyntaxAction(SyntaxNodeAnalysisContext context, INamedTypeSymbol taskSymbol, INamedTypeSymbol futureSymbol)
        {
            var expr = context.Node as AwaitExpressionSyntax;
            if (expr == null) return;

            var exprType = context.SemanticModel.GetTypeInfo(expr.Expression).ConvertedType;
            if (exprType == null) return;

            if (Equals(exprType, taskSymbol) || Equals(exprType, futureSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, expr.GetLocation()));
            }
        }
    }
}
