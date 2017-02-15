using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace AnalyzerInsecta
{
    public class Config
    {
        public IList<string> Analyzers { get; set; } = new List<string>();
        public IList<string> Projects { get; set; } = new List<string>();
        public string Output { get; set; }
        public bool GroupByProject { get; set; } = true;
        public bool GroupByDiagnosticId { get; set; } = true;
        public bool OpenOutput { get; set; }

        internal const string DefaultConfigFileName = "AnalyzerInsecta.json";

        internal static Config FromCommandLineOptions(CommandLineOptions cmdOptions)
        {
            if (cmdOptions == null) return new Config();

            var configFile = cmdOptions.ConfigFile;
            if (string.IsNullOrEmpty(configFile) && File.Exists(DefaultConfigFileName))
                configFile = DefaultConfigFileName;

            var config = new Config();
            var configDir = "";
            if (!string.IsNullOrEmpty(configFile))
            {
                configDir = Path.GetDirectoryName(configFile);
                using (var sr = new StreamReader(configFile))
                {
                    JsonSerializer.CreateDefault().Populate(sr, config);
                }
            }

            var analyzers = new List<string>();
            if (config.Analyzers != null)
            {
                analyzers.AddRange(config.Analyzers
                    .Select(x => Path.Combine(configDir, x)));
            }
            if (cmdOptions.Analyzers != null)
            {
                analyzers.AddRange(cmdOptions.Analyzers);
            }
            config.Analyzers = analyzers;

            var projects = new List<string>();
            if (config.Projects != null)
            {
                projects.AddRange(config.Projects
                    .Select(x => Path.Combine(configDir, x)));
            }
            if (cmdOptions.Projects != null)
            {
                projects.AddRange(cmdOptions.Projects);
            }
            config.Projects = projects;

            if (!string.IsNullOrEmpty(cmdOptions.Output))
                config.Output = cmdOptions.Output;

            return config;
        }
    }
}
