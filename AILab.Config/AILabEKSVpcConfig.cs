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
            //{"nova-DEV",new List<string>() { "subnet-01e7e7bda293d8785", "subnet-0d918c212f9ba2a81", "subnet-0e3a2c7c694c8601b" }},
            //{"nova-PRD", new List<string>() { "subnet-0304543452f554e20", "subnet-0304b609c266c4b60", "subnet-0e3eaf19af3b27b05" }},
            //{"yashbet-DEV", new List<string>() { "subnet-0dd11053ee9d25063", "subnet-021afa84bccb5a03d", "subnet-086d7ac1d1324cda0" }}, // 172.31.60.0/23、172.31.62.0/23、172.31.64.0/23
            {"jackfish-DEV", new List<string>() { "subnet-0d490330adca16246", "subnet-0baa06859306cb38e", "subnet-0ad50bdb3cfa68c0d" }} // 172.31.56.0/23、172.31.58.0/23、172.31.60.0/23
        };


        /// <summary>
        /// EKS Fargate Private Subnets
        /// </summary>
        private readonly Dictionary<string, List<string>> _eksFargatePrivateSubnets = new()
        {
            //{"nova-DEV", new List<string>() { "subnet-06fe44eb19505b841", "subnet-0399b3b00610100b0", "subnet-05620d1fe13307ced" }},
            //{"nova-PRD", new List<string>() { "subnet-01a9dc81e19da7e75", "subnet-0c105a92351a36fe0", "subnet-05c749b6de7db65c8" }},
            //{"yashbet-DEV", new List<string>() { "subnet-0f9ac904dc9332ec0", "subnet-0ff10df14709127ac", "subnet-062b060b5f6d37c72" }},  // 172.31.67.0/24、172.31.68.0/24、172.31.69.0/24
            {"jackfish-DEV", new List<string>() { "subnet-04a1c6e494a9473a4", "subnet-0bfd9a30b81d4e4a4", "subnet-0852b25a5a6195e92" }} // 172.31.80.0/23、172.31.82.0/23、172.31.84.0/23
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