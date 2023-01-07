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
                    "DEV-APP" => "Development.APP",
                    "PRD-APP" => "Production.APP",
                    _ => EnvironmentName
                };
            }
        }

        public string EFSFileSystemId
        {
            get
            {
                return EnvironmentName.Equals("PRD") ? PRD_EFSFileSystemId : DEV_EFSFileSystemId;
            }
        }

        private static readonly string DEV_EFSFileSystemId = "fs-04693ed0dab2989a9";//efs-ecs-dev

        private static readonly string PRD_EFSFileSystemId = "fs-04693ed0dab2989a9";//efs-ecs-dev

    }
}

