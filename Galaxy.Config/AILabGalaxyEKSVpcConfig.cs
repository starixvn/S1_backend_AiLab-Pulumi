namespace AILabGalaxyConfig
{
    public class AILabGalaxyEKSVpcConfig
    {
        public string EnvironmentName { get; }

        public AILabGalaxyEKSVpcConfig(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        #region EKS Cluster Private Subnets

        //public string[] PrivateSubnets_EKSCluster => EnvironmentName.Equals("PRD") ? PRD_PrivateSubnets_EKSCluster : DEV_PrivateSubnets_EKSCluster;
        public string[] PrivateSubnets_EKSCluster => DEV_PrivateSubnets_EKSCluster;

        private static readonly string[] DEV_PrivateSubnets_EKSCluster =
        {
            "subnet-0dd11053ee9d25063", //dev-subnet-private1a
            "subnet-021afa84bccb5a03d", //dev-subnet-private1b
            "subnet-086d7ac1d1324cda0", //dev-subnet-private1c
        };

        //private static readonly string[] PRD_PrivateSubnets_EKSCluster =
        //{
        //    "subnet-0304543452f554e20", //Private-PRD-1a
        //    "subnet-0304b609c266c4b60", //Private-PRD-1b
        //    "subnet-0e3eaf19af3b27b05", //Private-PRD-1c
        //};

        #endregion

        #region EKS Fargate Private Subnets

        //public string[] PrivateSubnets_EKSFargate => EnvironmentName.Equals("PRD") ? PRD_PrivateSubnets_EKSFargate : DEV_PrivateSubnets_EKSFargate;
        public string[] PrivateSubnets_EKSFargate => DEV_PrivateSubnets_EKSFargate;


        private static readonly string[] DEV_PrivateSubnets_EKSFargate =
        {
            "subnet-0f9ac904dc9332ec0", //Private-DEV-1a-eks-fargate，CIDR：172.31.67.0/24
            "subnet-0ff10df14709127ac", //Private-DEV-1b-eks-fargate，CIDR：172.31.68.0/24
            "subnet-062b060b5f6d37c72", //Private-DEV-1c-eks-fargate，CIDR：172.31.69.0/24
        };

        //private static readonly string[] PRD_PrivateSubnets_EKSFargate =
        //{
        //    "subnet-01a9dc81e19da7e75", //Private-PRD-1a-eks-fargate，CIDR：172.31.64.0/22
        //    "subnet-0c105a92351a36fe0", //Private-PRD-1b-eks-fargate，CIDR：172.31.68.0/22
        //    "subnet-05c749b6de7db65c8", //Private-PRD-1c-eks-fargate，CIDR：172.31.72.0/22
        //};

        #endregion
    }
}
