namespace AILabConfig
{
    public class AILabEKSVpcConfig
    {
        public string EnvironmentName { get; }

        public AILabEKSVpcConfig(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string[] PrivateSubnets_EKSCluster => EnvironmentName.Equals("PRD") ? PRD_PrivateSubnets_EKSCluster : DEV_PrivateSubnets_EKSCluster;

        private static readonly string[] DEV_PrivateSubnets_EKSCluster =
        {
            "subnet-01e7e7bda293d8785", //Private-DEV-1a
            "subnet-0d918c212f9ba2a81", //Private-DEV-1b
            "subnet-0e3a2c7c694c8601b", //Private-DEV-1c
        };

        private static readonly string[] PRD_PrivateSubnets_EKSCluster =
        {
            "subnet-0304543452f554e20", //Private-PRD-1a
            "subnet-0304b609c266c4b60", //Private-PRD-1b
            "subnet-0e3eaf19af3b27b05", //Private-PRD-1c
        };


        public string[] PrivateSubnets_EKSFargate => EnvironmentName.Equals("PRD") ? PRD_PrivateSubnets_EKSFargate : DEV_PrivateSubnets_EKSFargate;


        private static readonly string[] DEV_PrivateSubnets_EKSFargate =
        {
            "subnet-06fe44eb19505b841", //Private-DEV-1a-eks-fargate，CIDR：172.31.61.0/24
            "subnet-0399b3b00610100b0", //Private-DEV-1b-eks-fargate，CIDR：172.31.62.0/24
            "subnet-05620d1fe13307ced", //Private-DEV-1c-eks-fargate，CIDR：172.31.63.0/24
        };

        private static readonly string[] PRD_PrivateSubnets_EKSFargate =
        {
            "subnet-01a9dc81e19da7e75", //Private-PRD-1a-eks-fargate，CIDR：172.31.64.0/22
            "subnet-0c105a92351a36fe0", //Private-PRD-1b-eks-fargate，CIDR：172.31.68.0/22
            "subnet-05c749b6de7db65c8", //Private-PRD-1c-eks-fargate，CIDR：172.31.72.0/22
        };


    }
}
