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
    var farGateProfile = new Eks.FargateProfile("fargateProfile", new Eks.FargateProfileArgs()
    {
        ClusterName = eksCluster.Name,
        FargateProfileName = "fargateProfile",
        PodExecutionRoleArn = podExecutionRoleArn,
        SubnetIds = { vpcConfig.PrivateSubnets_EKSFargate },
        Selectors = { new FargateProfileSelectorArgs { Namespace = "default" } }
    });


    // create awsLoadBalancerControllerProfile for the eks aws load balancer controller
    var awsLoadBalancerControllerProfile = new Eks.FargateProfile("awsLoadBalancerControllerProfile", new Eks.FargateProfileArgs()
    {
        ClusterName = eksCluster.Name,
        FargateProfileName = "awsLoadBalancerControllerProfile",
        PodExecutionRoleArn = podExecutionRoleArn,
        SubnetIds = { vpcConfig.PrivateSubnets_EKSFargate },
        Selectors = { new FargateProfileSelectorArgs { Namespace = "kube-system" } }
    });

    // Create CoreDNS fargate Profile for the EKS cluster.
    var farGateProfile_CoreDNS = new Eks.FargateProfile("CoreDNS", new Eks.FargateProfileArgs()
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

});




