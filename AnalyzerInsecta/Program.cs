using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

            var compileOutputTemplateTask = Task.Run(new Func<OutputTemplateBase>(CompileOutputTemplate));

            var analyzerAssemblies = config.Analyzers
               .Select(Assembly.LoadFrom)
               .ToImmutableArray();

            var workspace = MSBuildWorkspace.Create(config.BuildProperties);
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
                    var diagnostics = analysisResult.GetAllDiagnostics();
                    var codeFixes = await codeFixRunner.RunCodeFixesAsync(x, diagnostics).ConfigureAwait(false);
                    return new ProjectAnalysisResult(x, diagnostics, analysisResult.AnalyzerTelemetryInfo, codeFixes);
                }))
            );

            const string defaultOutputFileName = "AnalyzerInsecta.html";
            var outputFilePath = config.Output;
            if (string.IsNullOrEmpty(outputFilePath))
                outputFilePath = defaultOutputFileName;
            else if (Directory.Exists(outputFilePath))
                outputFilePath = Path.Combine(outputFilePath, defaultOutputFileName);

            var outputDir = Path.GetDirectoryName(outputFilePath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

            var template = await compileOutputTemplateTask;
            using (var sw = new StreamWriter(outputFilePath, false, new UTF8Encoding(false)))
            {
                template.Writer = sw;
                await template.ExecuteAsync();
            }

            if (config.OpenOutput)
                Process.Start(outputFilePath);
        }

        private static OutputTemplateBase CompileOutputTemplate()
        {
            const string templateFileName = "OutputTemplate.cshtml";
            const string templateTypeName = "OutputTemplate";
            const string templateNamespace = "AnalyzerInsecta";

            GeneratorResults razorResult;
            using (var sr = new StreamReader(typeof(Program).Assembly.GetManifestResourceStream(typeof(Program), templateFileName)))
            {
                var host = new RazorEngineHost(new CSharpRazorCodeLanguage());
                host.DefaultBaseClass = nameof(OutputTemplateBase);
                var engine = new RazorTemplateEngine(host);
                razorResult = engine.GenerateCode(sr, templateTypeName, templateNamespace, templateFileName);
            }

            if (!razorResult.Success)
            {
                foreach (var error in razorResult.ParserErrors)
                    Console.Error.WriteLine(error);

                throw new Exception("Couldn't parse OutputTemplate.cshtml");
            }

            Assembly compiledAssembly;
            using (var asmStream = new MemoryStream())
            {
                var emitResult = CSharpCompilation
                    .Create(
                        templateTypeName,
                        new[] { CSharpSyntaxTree.ParseText(razorResult.GeneratedCode, path: "OutputTemplate.cs") },
                        new[]
                        {
                            MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // mscorlib
                            MetadataReference.CreateFromFile(typeof(Program).Assembly.Location) // AnalyzerInsecta
                        },
                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    )
                    .Emit(asmStream);

                if (!emitResult.Success)
                {
                    foreach (var diag in emitResult.Diagnostics)
                        Console.Error.WriteLine(diag);

                    throw new Exception("Couldn't compile OutputTemplate.cshtml");
                }

                compiledAssembly = Assembly.Load(asmStream.ToArray());
            }

            return (OutputTemplateBase)Activator.CreateInstance(
                compiledAssembly.GetType(templateNamespace + "." + templateTypeName, true));
        }
    }
}
