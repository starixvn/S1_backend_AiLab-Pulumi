namespace AILabConfig
{
    public class AILabAwsConfig
    {
        public AILabAwsConfig(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; }

        public string DockerRegistry { get; } = "http://13.213.82.69";

        public string DockerUsername { get; } = "ailab-aws";

        public string DockerPassword { get; } = "1ye7Bu46";

        public string ASPNETCORE_ENVIRONMENT
        {
            get
            {
                return EnvironmentName switch
                {
                    "PRD" => "Production",
                    "RLS" => "Release",
                    "DEV" => "Development",             
                    _ => EnvironmentName
                };
            }
        }
    }
}

