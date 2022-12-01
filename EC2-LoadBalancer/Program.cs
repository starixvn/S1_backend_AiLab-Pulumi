using System.Collections.Generic;
using AILabConfig;
using Pulumi;
using Aws = Pulumi.Aws;

return await Deployment.RunAsync(() =>
{
    //Config
    var config = new Config();
    var env = config.Require("ENV");
    var namePrefix = config.Require("NamePrefix");
    var shortName = $"{env}-{namePrefix}";

    var vpcConfig = new AILabVpcConfig(env);
    var SubnetMappings = new InputList<Aws.Alb.Inputs.LoadBalancerSubnetMappingArgs>();
    foreach (var subnet in vpcConfig.Subnets)
    {
        var mapping = new Aws.Alb.Inputs.LoadBalancerSubnetMappingArgs
        {
            SubnetId = subnet,
        };
        SubnetMappings.Add(mapping);
    }

    var loadBalancer = new Aws.Alb.LoadBalancer($"{shortName}-ALB", new()
    {
        LoadBalancerType = "application",
        Internal = true,
        SubnetMappings = SubnetMappings
    });

    return new Dictionary<string, object?>
    {
        ["loadBalancer"] = loadBalancer.GetResourceName(),
    };
});

