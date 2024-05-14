using AILabConfig;
using Pulumi;
using Pulumi.Aws.Eks.Inputs;
using Eks = Pulumi.Aws.Eks;

return await Deployment.RunAsync(() =>
{
    // Get Config
    var config = new Config();
    var env = config.Require("ENV");
    var brand = config.Require("ClusterName");
    var vpcConfig = new AILabEKSVpcConfig(env, brand);
    var namePrefix = config.Require("NamePrefix");
    var clusterName = $"{namePrefix}-{config.Require("ClusterName")}-{env.ToLower()}";
    var clusterRoleArn = config.Require("ClusterRoleArn");
    var publicAccessCidrs = config.Require("PublicAccessCidrs");

    // create eks cluster
    var eksCluster = new Eks.Cluster(clusterName, new Eks.ClusterArgs
    {
        Name = clusterName,
        RoleArn = clusterRoleArn,
        VpcConfig = new ClusterVpcConfigArgs
        {
            PublicAccessCidrs = { publicAccessCidrs },
            SubnetIds = { vpcConfig.PrivateSubnets_EKSCluster },
        }
    });

    // Create a fargate Profile for the EKS cluster.
    var podExecutionRoleArn = config.Require("PodExecutionRoleArn");

    
    var @default = new Eks.FargateProfile("default", new Eks.FargateProfileArgs()
    {
        ClusterName = eksCluster.Name,
        FargateProfileName = "default",
        PodExecutionRoleArn = podExecutionRoleArn,
        SubnetIds = { vpcConfig.PrivateSubnets_EKSFargate },
        Selectors = { new FargateProfileSelectorArgs { Namespace = "default" } }
    });

    
    var kubeSystem = new Eks.FargateProfile("Kube_system", new Eks.FargateProfileArgs()
    {
        ClusterName = eksCluster.Name,
        FargateProfileName = "Kube_system",
        PodExecutionRoleArn = podExecutionRoleArn,
        SubnetIds = { vpcConfig.PrivateSubnets_EKSFargate },
        Selectors = { new FargateProfileSelectorArgs { Namespace = "kube-system" } }
    });

    // Kube-system & label 
    var coreDns = new Eks.FargateProfile("CoreDNS", new Eks.FargateProfileArgs()
    {
        ClusterName = eksCluster.Name,
        FargateProfileName = "CoreDNS",
        PodExecutionRoleArn = podExecutionRoleArn,
        SubnetIds = { vpcConfig.PrivateSubnets_EKSFargate },
        Selectors = { new FargateProfileSelectorArgs
        {
            Namespace = "kube-system",
            Labels = new InputMap<string>
            {
                { "k8s-app", "kube-dns" }
            }
        }}
    });

    var yashbet = new Eks.FargateProfile("Yashbet", new Eks.FargateProfileArgs()
    {
        ClusterName = eksCluster.Name,
        FargateProfileName = "Yashbet",
        PodExecutionRoleArn = podExecutionRoleArn,
        SubnetIds = { vpcConfig.PrivateSubnets_EKSFargate },
        Selectors = { new FargateProfileSelectorArgs { Namespace = "yashbet" } }
    });

    
    var agentsite = new Eks.FargateProfile("AgentSite", new Eks.FargateProfileArgs()
    {
        ClusterName = eksCluster.Name,
        FargateProfileName = "AgentSite",
        PodExecutionRoleArn = podExecutionRoleArn,
        SubnetIds = { vpcConfig.PrivateSubnets_EKSFargate },
        Selectors = { new FargateProfileSelectorArgs { Namespace = "agentsite" } }
    });
});



