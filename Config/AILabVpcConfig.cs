namespace AILabConfig
{
    public class AILabVpcConfig
    {
        public AILabVpcConfig(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; }

        public string VpcId { get; } = "vpc-10889e77";

        public string[] Subnets 
        { 
            get
            {
                return EnvironmentName.Equals("PRD") ? PRD_Subnets : DEV_Subnets;
            }
        }

        private static readonly string[] DEV_Subnets = 
        { 
            "subnet-01e7e7bda293d8785", 
            "subnet-0d918c212f9ba2a81", 
            "subnet-0e3a2c7c694c8601b" 
        };

        private static readonly string[] PRD_Subnets = 
        {
            "subnet-01e7e7bda293d8785", 
            "subnet-0d918c212f9ba2a81", 
            "subnet-0e3a2c7c694c8601b" 
        };
    }
}

