using AILabConfig;
using Pulumi;
using Pulumi.Aws.Eks.Inputs;
using Pulumi.Aws.Iam;
using Eks = Pulumi.Aws.Eks;


return await Deployment.RunAsync(() =>
{
    //Config
    var config = new Config();
    var env = config.Require("ENV");
    var vpcConfig = new AILabVpcConfig(env);
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
            SubnetIds = { vpcConfig.PrivateSubnets },
        }
    });

    ////// Create a farGate Profile for the EKS cluster.
    //var farGateProfile = new Eks.FargateProfile("farGateProfile", new Eks.FargateProfileArgs()
    //{
    //    ClusterName = eksCluster.Name,
    //    FargateProfileName = "farGateProfile",
    //    PodExecutionRoleArn = "arn:aws:iam::761771912479:role/aws-service-role/eks-fargate.amazonaws.com/AWSServiceRoleForAmazonEKSForFargate",
    //    SubnetIds = { vpcConfig.PrivateSubnets },
    //    Selectors = { new FargateProfileSelectorArgs { Namespace = "nova" } }
    //});
});




