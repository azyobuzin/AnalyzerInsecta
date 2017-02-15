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
                config.Projects
                    .Select(x => workspace.OpenProjectAsync(x))
            );

            var analyzerRunner = new AnalyzerRunner();

            foreach (var x in analyzerAssemblies)
                analyzerRunner.RegisterAnalyzersFromAssembly(x);

            var analysisResults = await Task.WhenAll(
                projects.Select(async x =>
                    await analyzerRunner.RunAnalyzersAsync(
                        await x.GetCompilationAsync()
                    )
                )
            );
        }
    }
}
