using System;
using System.Collections.Concurrent;
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
            var supportedCodeFixProviders = this._codeFixProviders.FindAll(x => x.Languages.Contains(project.Language));
            if (supportedCodeFixProviders.Count == 0) return ImmutableArray<CodeFixResult>.Empty;

            var waitingTasks = new List<Task<CodeFixResult[]>>();

            // 同じ TextSpan のものをグルーピング
            var groups = diagnostics.Where(x => x.Location.IsInSource && x.Location.SourceTree != null)
                .GroupBy(x => new DocumentSpan(project.GetDocument(x.Location.SourceTree), x.Location.SourceSpan));

            foreach (var g in groups)
            {
                var span = g.Key;

                foreach (var p in supportedCodeFixProviders)
                {
                    var supportedDiagnosticIds = p.Instance.FixableDiagnosticIds;
                    var targetDiagnostics = g.Where(x => supportedDiagnosticIds.Contains(x.Id)).ToImmutableArray();

                    if (targetDiagnostics.Length > 0)
                    {
                        waitingTasks.Add(Task.Run(async () =>
                        {
                            var registerCodeFixTasks = new ConcurrentBag<Task<CodeFixResult>>();

                            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix = (codeAction, ds) =>
                            {
                                if (registerCodeFixTasks == null)
                                    throw new InvalidOperationException("RegisterCodeFix was called outside of RegisterCodeFixesAsync.");

                                // RegisterCodeFix は RegisterCodeFixesAsync が終了する前に呼びされるので
                                // このタイミングで待機タスクリストに突っ込んでおく
                                registerCodeFixTasks.Add(Task.Run(async () =>
                                {
                                    var operations = await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false);

                                    if (operations.Length == 0) return null;

                                    ChangedDocument? changedDocument = null;

                                    if (operations.Length == 1)
                                    {
                                        var firstOperation = operations[0] as ApplyChangesOperation;
                                        if (firstOperation != null)
                                        {
                                            changedDocument = TryGetChangedDocument(project, firstOperation.ChangedSolution);
                                        }
                                    }

                                    return new CodeFixResult(
                                        p.Name,
                                        codeAction.Title,
                                        ds,
                                        span,
                                        changedDocument
                                    );
                                }, cancellationToken));
                            };

                            await p.Instance
                                .RegisterCodeFixesAsync(new CodeFixContext(
                                    span.Document,
                                    span.TextSpan,
                                    targetDiagnostics,
                                    registerCodeFix,
                                    cancellationToken
                                ))
                                .ConfigureAwait(false);

                            // RegisterCodeFixesAsync 終了後に RegisterCodeFix が呼びされた場合に
                            // 例外を出すために、ここで registerCodeFixTasks を入れ替えておく
                            var tasks = registerCodeFixTasks;
                            registerCodeFixTasks = null;
                            return await Task.WhenAll(tasks).ConfigureAwait(false);
                        }, cancellationToken));
                    }
                }
            }

            return (await Task.WhenAll(waitingTasks).ConfigureAwait(false))
                .SelectMany(x => x)
                .Where(x => x != null)
                .ToImmutableArray();
        }

        private static ChangedDocument? TryGetChangedDocument(Project baseProject, Solution changedSolution)
        {
            var solutionChanges = changedSolution.GetChanges(baseProject.Solution);

            if (solutionChanges.GetAddedProjects().Any() || solutionChanges.GetRemovedProjects().Any())
                return null;

            var changedProjects = solutionChanges.GetProjectChanges().ToArray();
            if (changedProjects.Length != 1) return null;

            var projectChanges = changedProjects[0];

            if (projectChanges.OldProject != baseProject
                || projectChanges.GetAddedAdditionalDocuments().Any()
                || projectChanges.GetAddedAnalyzerReferences().Any()
                || projectChanges.GetAddedDocuments().Any()
                || projectChanges.GetAddedMetadataReferences().Any()
                || projectChanges.GetAddedProjectReferences().Any()
                || projectChanges.GetChangedAdditionalDocuments().Any()
                || projectChanges.GetRemovedAdditionalDocuments().Any()
                || projectChanges.GetRemovedAnalyzerReferences().Any()
                || projectChanges.GetRemovedDocuments().Any()
                || projectChanges.GetRemovedMetadataReferences().Any()
                || projectChanges.GetRemovedProjectReferences().Any())
                return null;

            var changedDocuments = projectChanges.GetChangedDocuments().ToArray();
            if (changedDocuments.Length != 1) return null;

            return new ChangedDocument(
                projectChanges.OldProject.GetDocument(changedDocuments[0]),
                projectChanges.NewProject.GetDocument(changedDocuments[0])
            );
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
