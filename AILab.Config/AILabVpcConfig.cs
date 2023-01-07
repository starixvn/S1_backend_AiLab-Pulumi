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

        public string[] PrivateSubnets 
        { 
            get
            {
                return EnvironmentName.Equals("PRD") ? PRD_PrivateSubnets : DEV_PrivateSubnets;
            }
        }
  
        private static readonly string[] DEV_PrivateSubnets = 
        { 
            "subnet-01e7e7bda293d8785", //Private-DEV-1a
            "subnet-0d918c212f9ba2a81", //Private-DEV-1b
            "subnet-0e3a2c7c694c8601b", //Private-DEV-1c
        };

        private static readonly string[] PRD_PrivateSubnets = 
        {
            "subnet-0304543452f554e20", //Private-PRD-1a
            "subnet-0304b609c266c4b60", //Private-PRD-1b
            "subnet-0e3eaf19af3b27b05", //Private-PRD-1c
        };

        public string[] PublicSubnets
        {
            get
            {
                return new string[]
                {
                    "subnet-09332247c64aff074", //Public-1a
                    "subnet-01756b250ebae0fae", //Public-1b
                    "subnet-0ea1391c518fe60dc", //Public-1c
                };
            }
        }
    }
}

