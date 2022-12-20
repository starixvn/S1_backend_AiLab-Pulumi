namespace AILabConfig
{
    public class AILabAwsConfig
    {
        public AILabAwsConfig(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; }

        public string DockerRegistry { get; } = "harbor.ailabpte.co";

        public string AWSSecretsManagerName { get; } = "harbor.ailabpte.co";

        public string TaskRoleArn { get; } = "arn:aws:iam::761771912479:role/ecsTaskExecutionRole";

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

