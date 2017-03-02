using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SampleAnalyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InefficientStringInterpolationAnalyzerCodeFixProvider))]
    [Shared]
    public class InefficientStringInterpolationAnalyzerCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(InefficientStringInterpolationAnalyzer.Rule.Id);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var doc = context.Document;
            var root = await doc.GetSyntaxRootAsync().ConfigureAwait(false);

            var expr = root.FindToken(context.Span.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<InterpolatedStringExpressionSyntax>()
                .FirstOrDefault();

            if (expr == null) return;

            Func<Func<InterpolatedStringExpressionSyntax, SyntaxNode>, CancellationToken, Task<Document>> fix =
                (converter, cancellationToken) => Task.FromResult(
                    doc.WithSyntaxRoot(root.ReplaceNode(expr, converter(expr)))
                );

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use string.Concat",
                    x => fix(ToStringConcat, x),
                    nameof(InefficientStringInterpolationAnalyzerCodeFixProvider) + "." + nameof(ToStringConcat)
                ),
                context.Diagnostics
            );

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use + operators",
                    x => fix(ToPlusOperators, x),
                    nameof(InefficientStringInterpolationAnalyzerCodeFixProvider) + "." + nameof(ToPlusOperators)
                ),
                context.Diagnostics
            );
        }

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        private static SyntaxNode ToStringConcat(InterpolatedStringExpressionSyntax expr)
        {
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                    SyntaxFactory.IdentifierName("Concat")
                ),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                    GetContentExpressions(expr).Select(SyntaxFactory.Argument)
                ))
            );
        }

        private static SyntaxNode ToPlusOperators(InterpolatedStringExpressionSyntax expr)
        {
            return GetContentExpressions(expr)
                .Aggregate((ae, content) =>
                    SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, ae, content)
                );
        }

        private static IEnumerable<ExpressionSyntax> GetContentExpressions(InterpolatedStringExpressionSyntax expr)
        {
            return expr.Contents
                .Select(x =>
                {
                    var stringLiteral = x as InterpolatedStringTextSyntax;
                    if (stringLiteral != null)
                    {
                        return SyntaxFactory.LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(stringLiteral.TextToken.ValueText)
                        );
                    }

                    return ((InterpolationSyntax)x).Expression;
                });
        }
    }
}
