using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;

namespace AnalyzerInsecta
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var cmdOptions = new CommandLineOptions();
            CommandLine.Parser.Default.ParseArgumentsStrict(args, cmdOptions);

            var config = Config.FromCommandLineOptions(cmdOptions);

            Run(config).GetAwaiter().GetResult();
        }

        private static async Task Run(Config config)
        {
            if (config.Analyzers.Count == 0)
            {
                Console.WriteLine("No analyzers");
                return;
            }

            if (config.Projects.Count == 0)
            {
                Console.WriteLine("No projects");
                return;
            }

            var analyzerAssemblies = config.Analyzers
               .Select(Assembly.LoadFrom)
               .ToImmutableArray();

            var workspace = MSBuildWorkspace.Create();
            var projects = await Task.WhenAll(
                config.Projects.Select(x =>
                    Task.Run(() => workspace.OpenProjectAsync(x))
                )
            );

            var analyzerRunner = new AnalyzerRunner();
            var codeFixRunner = new CodeFixRunner();

            foreach (var x in analyzerAssemblies)
            {
                analyzerRunner.RegisterAnalyzersFromAssembly(x);
                codeFixRunner.RegisterCodeFixProvidersFromAssembly(x);
            }

            var analysisResults = await Task.WhenAll(
                projects.Select(x => Task.Run(async () =>
                {
                    var compilation = await x.GetCompilationAsync().ConfigureAwait(false);
                    var analysisResult = await analyzerRunner.RunAnalyzersAsync(compilation).ConfigureAwait(false);
                    return new ProjectAnalysisResult(x, analysisResult.GetAllDiagnostics(), analysisResult.AnalyzerTelemetryInfo);
                }))
            );
        }
    }
}
