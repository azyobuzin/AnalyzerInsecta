using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace AnalyzerInsecta
{
    public class CodeFixRunner
    {
        private readonly List<CodeFixProviderInfo> _codeFixProviders = new List<CodeFixProviderInfo>();

        public void RegisterCodeFixProvidersFromAssembly(Assembly assembly)
        {
            this._codeFixProviders.AddRange(
                assembly.GetTypes()
                    .Where(x => x.IsSubclassOf(typeof(CodeFixProvider)))
                    .Select(x => new
                    {
                        Type = x,
                        Attr = x.GetCustomAttribute<ExportCodeFixProviderAttribute>()
                    })
                    .Where(x => x.Attr?.Languages != null && x.Attr.Languages.Length > 0)
                    .Select(x => new CodeFixProviderInfo(
                        (CodeFixProvider)Activator.CreateInstance(x.Type),
                        string.IsNullOrEmpty(x.Attr.Name) ? x.Type.Name : x.Attr.Name,
                        x.Attr.Languages
                    ))
            );
        }

        public async Task<ImmutableArray<CodeFixResult>> RunCodeFixesAsync(Project project, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = ImmutableArray.CreateBuilder<CodeFixResult>();
            var waitingTasks = new List<Task>();

            // Find the same spans
            var groups = diagnostics.Where(x => x.Location.IsInSource && x.Location.SourceTree != null)
                .GroupBy(x => Tuple.Create(project.GetDocument(x.Location.SourceTree), x.Location.SourceSpan));

            foreach (var g in groups)
            {
                var span = new DocumentSpan(g.Key.Item1, g.Key.Item2);

                foreach (var p in this._codeFixProviders.Where(x => x.Languages.Contains(project.Language)))
                {
                    var supportedDiagnosticIds = p.Instance.FixableDiagnosticIds;
                    var targetDiagnostics = g.Where(x => supportedDiagnosticIds.Contains(x.Id)).ToImmutableArray();

                    if (targetDiagnostics.Length > 0)
                    {
                        await p.Instance
                            .RegisterCodeFixesAsync(new CodeFixContext(
                                span.Document,
                                span.TextSpan,
                                targetDiagnostics,
                                (codeAction, _) => waitingTasks.Add(Task.Run(async () =>
                                {
                                    foreach (var op in await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false))
                                    {
                                        var aco = op as ApplyChangesOperation;
                                        if (aco != null)
                                        {
                                            // TODO
                                            // というかやはりここに長々と書くの汚い
                                        }
                                        else
                                        {
                                            result.Add(new CodeFixResult(
                                                p.Name,
                                                codeAction.Title,
                                                span,
                                                false,
                                                default(ImmutableArray<ChangedDocument>)
                                            ));
                                        }
                                    }
                                }, cancellationToken)),
                                cancellationToken
                            ))
                            .ConfigureAwait(false);
                    }
                }
            }

            await Task.WhenAll(waitingTasks).ConfigureAwait(false);

            return result.ToImmutable();
        }

        private struct CodeFixProviderInfo
        {
            public CodeFixProvider Instance { get; }
            public string Name { get; }
            public string[] Languages { get; }

            public CodeFixProviderInfo(CodeFixProvider instance, string name, string[] languages)
            {
                this.Instance = instance;
                this.Name = name;
                this.Languages = languages;
            }
        }
    }
}
