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

        /// <summary>
        /// Pulumi執行帳號的Account ID
        /// </summary>
        public string PulumiOrganization { get; } = "ailab-backend";

        /// <summary>
        /// Pulumi的EC2-LoadBalancer專案名稱
        /// </summary>
        public string LoadBalancerProject { get; } = "EC2-LoadBalancer";

        /// <summary>
        /// Pulumi的EC2-TargetGroup專案名稱
        /// </summary>
        public string TargetGroupProject { get; } = "EC2-TargetGroup";

        /// <summary>
        /// Pulumi的ECS-Cluster專案名稱
        /// </summary>
        public string ECSClusterProject { get; } = "ECS-Cluster";

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

