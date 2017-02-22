using CommandLine;

namespace AnalyzerInsecta
{
    internal class CommandLineOptions
    {
        [Option("config", HelpText = "A path to a configuration file")]
        public string ConfigFile { get; set; }

        [OptionArray('a', "analyzers", HelpText = "A list of paths to assembly files that contain analyzers")]
        public string[] Analyzers { get; set; }

        [OptionArray("projects", HelpText = "A list of paths to C# or VB project files")]
        public string[] Projects { get; set; }

        [OptionArray("properties", HelpText = "A list of properties passed to MSBuild")]
        public string[] BuildProperties { get; set; }

        [Option('o', "output", HelpText = "A path to the output file or directory")]
        public string Output { get; set; }
    }
}
