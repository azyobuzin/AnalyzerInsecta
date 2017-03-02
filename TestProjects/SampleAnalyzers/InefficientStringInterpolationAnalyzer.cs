using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SampleAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InefficientStringInterpolationAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            "Sample0001", "Inefficient String Interpolation",
            "This can be a simple concatenation.", "Sample",
            DiagnosticSeverity.Info, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(
                InterpolatedStringExpressionSyntaxAction,
                SyntaxKind.InterpolatedStringExpression
            );
        }

        private static void InterpolatedStringExpressionSyntaxAction(SyntaxNodeAnalysisContext context)
        {
            var expr = context.Node as InterpolatedStringExpressionSyntax;
            if (expr == null) return;

            var interpolations = expr.Contents.OfType<InterpolationSyntax>().ToArray();
            if (interpolations.Length == 0) return;

            var stringTypeSymbol = context.Compilation.GetSpecialType(SpecialType.System_String);

            if (stringTypeSymbol == null) return; // Invalid compilation

            foreach (var x in interpolations)
            {
                if (x.AlignmentClause != null || x.FormatClause != null) return;

                var exprType = context.SemanticModel.GetTypeInfo(x.Expression).ConvertedType;
                if (!ReferenceEquals(exprType, stringTypeSymbol)) return;
            }

            // Now all interpolations are plain strings
            context.ReportDiagnostic(Diagnostic.Create(Rule, expr.GetLocation()));
        }
    }
}
