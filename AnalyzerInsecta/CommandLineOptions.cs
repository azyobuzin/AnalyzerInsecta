using CommandLine;

namespace AnalyzerInsecta
{
    internal class CommandLineOptions
    {
        [Option("config")]
        public string ConfigFile { get; set; }

        [OptionArray('a', "analyzers")]
        public string[] Analyzers { get; set; }

        [OptionArray("projects")]
        public string[] Projects { get; set; }

        [Option('o', "output")]
        public string Output { get; set; }
    }
}
