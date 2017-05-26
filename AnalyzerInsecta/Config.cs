using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace AnalyzerInsecta
{
    public class Config
    {
        public IList<string> Analyzers { get; set; } = new List<string>();
        public IList<string> Projects { get; set; } = new List<string>();
        public IDictionary<string, string> BuildProperties { get; set; } = new Dictionary<string, string>();
        public string Output { get; set; }
        public bool OpenOutput { get; set; }
        public bool AttachDebugger { get; set; }

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
                    JsonSerializer.Create().Populate(sr, config);
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

            var buildProperties = new Dictionary<string, string>();
            if (config.BuildProperties != null)
            {
                foreach (var kvp in config.BuildProperties)
                {
                    if (string.IsNullOrEmpty(kvp.Key))
                        throw new InvalidOperationException("There is the element of BuildProperties whose key is null.");
                    buildProperties.Add(kvp.Key, kvp.Value);
                }
            }
            if (cmdOptions.BuildProperties != null)
            {
                foreach (var s in cmdOptions.BuildProperties)
                {
                    if (string.IsNullOrEmpty(s)) continue;

                    var split = s.Split(new[] { '=' }, 2);
                    Contract.Assert(split.Length <= 2);

                    if (split.Length >= 2 && split[0].Length == 0)
                    {
                        Console.Error.WriteLine($"Invalid property: \"{s}\"");
                        continue;
                    }

                    buildProperties[split[0]] = split.Length == 1 ? null : split[1];
                }
            }
            config.BuildProperties = buildProperties;

            if (!string.IsNullOrEmpty(cmdOptions.Output))
                config.Output = cmdOptions.Output;
            else if (!string.IsNullOrEmpty(config.Output))
                config.Output = Path.Combine(configDir, config.Output);

            config.AttachDebugger = cmdOptions.AttachDebugger;

            return config;
        }
    }
}
