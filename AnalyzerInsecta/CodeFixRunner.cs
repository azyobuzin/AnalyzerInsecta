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
            var waitingTasks = new List<Task<CodeFixResult[]>>();

            // Find the same spans
            var groups = diagnostics.Where(x => x.Location.IsInSource && x.Location.SourceTree != null)
                .GroupBy(x => new DocumentSpan(project.GetDocument(x.Location.SourceTree), x.Location.SourceSpan));

            foreach (var g in groups)
            {
                var span = g.Key;

                foreach (var p in this._codeFixProviders.Where(x => x.Languages.Contains(project.Language)))
                {
                    var supportedDiagnosticIds = p.Instance.FixableDiagnosticIds;
                    var targetDiagnostics = g.Where(x => supportedDiagnosticIds.Contains(x.Id)).ToImmutableArray();

                    if (targetDiagnostics.Length > 0)
                    {
                        waitingTasks.Add(Task.Run(async () =>
                        {
                            var registerCodeFixTasks = new List<Task<CodeFixResult>>();

                            Action<CodeAction, ImmutableArray<Diagnostic>> registerCodeFix = (codeAction, ds) =>
                            {
                                registerCodeFixTasks.Add(Task.Run(async () =>
                                {
                                    var operations = await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false);

                                    if (operations.Length == 0) return null;

                                    var isSupported = false;
                                    var changes = default(ImmutableArray<ChangedDocument>);

                                    if (operations.Length == 1)
                                    {
                                        var firstOperation = operations[0] as ApplyChangesOperation;
                                        if (firstOperation != null)
                                        {
                                            isSupported = TryGetChangedDocuments(project, firstOperation.ChangedSolution, out changes);
                                        }
                                    }

                                    return new CodeFixResult(
                                        p.Name,
                                        codeAction.Title,
                                        ds,
                                        span,
                                        isSupported,
                                        changes
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

                            return await Task.WhenAll(registerCodeFixTasks).ConfigureAwait(false);
                        }, cancellationToken));
                    }
                }
            }

            return (await Task.WhenAll(waitingTasks).ConfigureAwait(false))
                .SelectMany(x => x)
                .Where(x => x != null)
                .ToImmutableArray();
        }

        private static bool TryGetChangedDocuments(Project baseProject, Solution changedSolution, out ImmutableArray<ChangedDocument> result)
        {
            result = default(ImmutableArray<ChangedDocument>);

            var solutionChanges = changedSolution.GetChanges(baseProject.Solution);

            // The solution has been changed.
            if (solutionChanges.GetAddedProjects().Any() || solutionChanges.GetRemovedProjects().Any())
                return false;

            var changedProjects = solutionChanges.GetProjectChanges().ToArray();

            if (changedProjects.Length == 0)
            {
                result = ImmutableArray<ChangedDocument>.Empty;
                return true;
            }

            // Other projects have been changed.
            if (changedProjects.Length > 1) return false;

            var projectChanges = changedProjects[0];

            if (projectChanges.OldProject != baseProject
                || projectChanges.GetAddedAdditionalDocuments().Any()
                || projectChanges.GetAddedAnalyzerReferences().Any()
                || projectChanges.GetAddedMetadataReferences().Any()
                || projectChanges.GetAddedProjectReferences().Any()
                || projectChanges.GetChangedAdditionalDocuments().Any()
                || projectChanges.GetRemovedAdditionalDocuments().Any()
                || projectChanges.GetRemovedAnalyzerReferences().Any()
                || projectChanges.GetRemovedMetadataReferences().Any()
                || projectChanges.GetRemovedProjectReferences().Any())
                return false;

            var newProject = projectChanges.NewProject;

            result = projectChanges.GetAddedDocuments()
                .Select(x => new ChangedDocument(null, newProject.GetDocument(x)))
                .Concat(
                    projectChanges.GetChangedDocuments()
                        .Select(x => new ChangedDocument(
                            baseProject.GetDocument(x),
                            newProject.GetDocument(x)
                        ))
                )
                .Concat(
                    projectChanges.GetRemovedDocuments()
                        .Select(x => new ChangedDocument(baseProject.GetDocument(x), null))
                )
                .ToImmutableArray();

            return true;
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
