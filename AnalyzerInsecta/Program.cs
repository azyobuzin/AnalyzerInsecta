﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Newtonsoft.Json;

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

            if (config.AttachDebugger) Debugger.Launch();

            var (analyzerRunner, codeFixRunner) = await Phase(
                "Loading analyzers",
                () => Task.FromResult(LoadAnalyzers(config))
            );

            var analysisResults = await Phase(
                "Running analysises",
                () => RunAnalysises(config, analyzerRunner, codeFixRunner)
            );

            analysisResults = Array.FindAll(analysisResults, x => x != null);

            if (analysisResults.Length == 0)
            {
                Console.WriteLine("No output");
                return;
            }

            var outputFilePath = await Phase(
                "Writing result",
                () => WriteOutput(config, analysisResults)
            );

            Console.WriteLine("Done: " + outputFilePath);

            if (config.OpenOutput)
                Process.Start(outputFilePath);
        }

        private static async Task<T> Phase<T>(string message, Func<Task<T>> action)
        {
            Console.Write(message);
            var stopwatch = Stopwatch.StartNew();

            var result = await action().ConfigureAwait(false);

            stopwatch.Stop();
            Console.WriteLine(" " + stopwatch.Elapsed);

            return result;
        }

        private static (AnalyzerRunner, CodeFixRunner) LoadAnalyzers(Config config)
        {
            var analyzerRunner = new AnalyzerRunner();
            var codeFixRunner = new CodeFixRunner();

            foreach (var x in config.Analyzers)
            {
                var asm = Assembly.LoadFrom(x);
                analyzerRunner.RegisterAnalyzersFromAssembly(asm);
                codeFixRunner.RegisterCodeFixProvidersFromAssembly(asm);
            }

            return (analyzerRunner, codeFixRunner);
        }

        private static Task<ProjectAnalysisResult[]> RunAnalysises(Config config, AnalyzerRunner analyzerRunner, CodeFixRunner codeFixRunner)
        {
            var workspace = MSBuildWorkspace.Create(config.BuildProperties);

            workspace.WorkspaceFailed += (sender, e) =>
            {
                Console.ForegroundColor = e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure
                    ? ConsoleColor.Red
                    : ConsoleColor.DarkYellow;
                Console.Error.WriteLine(e.Diagnostic);
                Console.ResetColor();
            };

            return Task.WhenAll(
                config.Projects
                .Select(x => Task.Run(async () =>
                {
                    try
                    {
                        var project = await workspace.OpenProjectAsync(x).ConfigureAwait(false);
                        var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
                        var analysisResult = await analyzerRunner.RunAnalyzersAsync(compilation).ConfigureAwait(false);
                        var diagnostics = analysisResult.GetAllDiagnostics();
                        var codeFixes = await codeFixRunner.RunCodeFixesAsync(project, diagnostics).ConfigureAwait(false);
                        return new ProjectAnalysisResult(project, diagnostics, analysisResult.AnalyzerTelemetryInfo, codeFixes);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Error.WriteLine("Failed to load " + Path.GetFileName(x) + ": " + ex.ToString());
                        Console.ResetColor();
                        return null;
                    }
                }))
            );
        }

        private static async Task<string> WriteOutput(Config config, ProjectAnalysisResult[] analysisResults)
        {
            const string defaultOutputFileName = "AnalyzerInsecta.html";
            var outputFilePath = config.Output;
            if (string.IsNullOrEmpty(outputFilePath))
                outputFilePath = defaultOutputFileName;
            else if (Directory.Exists(outputFilePath))
                outputFilePath = Path.Combine(outputFilePath, defaultOutputFileName);

            var outputDir = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

            var model = await OutputGenerator.CreateModel(analysisResults).ConfigureAwait(false);

            using (var outputStream = File.OpenWrite(outputFilePath))
            {
                using (var headStream = OpenResource("head.html"))
                    headStream.CopyTo(outputStream);

                using (var sw = new StreamWriter(outputStream, new UTF8Encoding(false), 1024, true))
                {
                    var serializer = JsonSerializer.Create();
                    serializer.Serialize(sw, model);
                }

                using (var tailStream = OpenResource("tail.html"))
                    tailStream.CopyTo(outputStream);
            }

            return outputFilePath;
        }

        private static Stream OpenResource(string resourceName)
        {
            return typeof(Program).Assembly.GetManifestResourceStream(typeof(Program), resourceName);
        }
    }
}
