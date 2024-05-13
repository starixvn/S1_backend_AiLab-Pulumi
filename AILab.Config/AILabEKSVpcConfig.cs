namespace AILabConfig
{
    public class AILabEKSVpcConfig
    {
        #region vpc setting

        /// <summary>
        /// EKS Cluster Private Subnets
        /// </summary>
        private readonly Dictionary<string, List<string>> _eksClusterPrivateSubnets = new()
        {
            {"nova-DEV",new List<string>() { "subnet-01e7e7bda293d8785", "subnet-0d918c212f9ba2a81", "subnet-0e3a2c7c694c8601b" }},
            {"nova-PRD", new List<string>() { "subnet-0304543452f554e20", "subnet-0304b609c266c4b60", "subnet-0e3eaf19af3b27b05" }}
        };

        /// <summary>
        /// EKS Fargate Private Subnets
        /// </summary>
        private readonly Dictionary<string, List<string>> _eksFargatePrivateSubnets = new()
        {
            {"nova-DEV", new List<string>() { "subnet-06fe44eb19505b841", "subnet-0399b3b00610100b0", "subnet-05620d1fe13307ced" }},
            {"nova-PRD", new List<string>() { "subnet-01a9dc81e19da7e75", "subnet-0c105a92351a36fe0", "subnet-05c749b6de7db65c8" }}
        };

        #endregion

        public string EnvironmentName { get; }
        public string[] PrivateSubnets_EKSCluster { get; set; }
        public string[] PrivateSubnets_EKSFargate { get; set; }

        public AILabEKSVpcConfig(string environmentName, string brand)
        {
            EnvironmentName = environmentName;
            PrivateSubnets_EKSCluster = _eksClusterPrivateSubnets[$"{brand}-{environmentName}"].ToArray();
            PrivateSubnets_EKSFargate = _eksFargatePrivateSubnets[$"{brand}-{environmentName}"].ToArray();
        }
    }
}