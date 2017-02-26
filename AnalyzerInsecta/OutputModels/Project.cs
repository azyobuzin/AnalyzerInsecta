namespace AnalyzerInsecta.OutputModels
{
    public class Project
    {
        public string Name { get; set; }
        public Language Language { get; set; }
        public Telemetry[] TelemetryInfo { get; set; }

        public Project() { }

        public Project(string name, Language language, Telemetry[] telemetryInfo)
        {
            this.Name = name;
            this.Language = language;
            this.TelemetryInfo = telemetryInfo;
        }
    }
}
