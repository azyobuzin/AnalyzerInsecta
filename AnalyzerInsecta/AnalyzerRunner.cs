using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AnalyzerInsecta
{
    public class AnalyzerRunner
    {
        private readonly List<AnalyzerInfo> _analyzers = new List<AnalyzerInfo>();

        public void RegisterAnalyzersFromAssembly(Assembly assembly)
        {
            this._analyzers.AddRange(
                assembly.GetTypes()
                    .Where(x => x.IsSubclassOf(typeof(DiagnosticAnalyzer)))
                    .Select(x => new
                    {
                        Type = x,
                        x.GetCustomAttribute<DiagnosticAnalyzerAttribute>()?.Languages
                    })
                    .Where(x => x.Languages != null && x.Languages.Length > 0)
                    .Select(x => new AnalyzerInfo(
                        (DiagnosticAnalyzer)Activator.CreateInstance(x.Type),
                        x.Languages
                    ))
            );
        }

        private ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string languageName)
        {
            return this._analyzers
                .Where(x => x.Languages.Contains(languageName))
                .Select(x => x.Instance)
                .ToImmutableArray();
        }

        public Task<AnalysisResult> RunAnalyzersAsync(Compilation compilation, CancellationToken cancellationToken = default(CancellationToken))
        {
            return compilation.WithAnalyzers(this.GetAnalyzers(compilation.Language))
                .GetAnalysisResultAsync(cancellationToken);
        }

        private struct AnalyzerInfo
        {
            public DiagnosticAnalyzer Instance { get; }
            public string[] Languages { get; }

            public AnalyzerInfo(DiagnosticAnalyzer instance, string[] languages)
            {
                this.Instance = instance;
                this.Languages = languages;
            }
        }
    }
}
