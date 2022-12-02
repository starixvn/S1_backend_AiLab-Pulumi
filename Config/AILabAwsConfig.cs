namespace AILabConfig
{
    public class AILabAwsConfig
    {
        public AILabAwsConfig(string environmentName)
        {
            EnvironmentName = environmentName;
        }
        public string EnvironmentName { get; }
        public string KeyName { get; } = "ailab-linux";
        public string ImageId { get; } = "ami-0b91f3efad17de577";
        public string DockerRegistry { get; } = "13.213.82.69";
        public string InstanceType
        {
            get
            {
                return EnvironmentName.Equals("PRD") ? PRD_InstanceType : DEV_InstanceType;
            }
        }
        private static readonly string DEV_InstanceType = "t3a.medium";
        private static readonly string PRD_InstanceType = "t3a.medium";
    }
}

