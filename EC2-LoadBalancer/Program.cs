using System;
using System.Collections.Generic;
using AILabConfig;
using Pulumi;
using Aws = Pulumi.Aws;
using Awsx = Pulumi.Awsx;

return await Deployment.RunAsync(() =>
{
    //Config
    var config = new Config();
    var env = config.Require("ENV");
    var namePrefix = config.Require("NamePrefix");
    var shortName = $"{env}-{namePrefix}";
    var tags = new InputMap<string>()
    {
         { "Name", shortName},
         { "Create-By", "Pulumi"},
         { "Create-Time", DateTimeOffset.UtcNow.ToOffset(new TimeSpan(8,0,0)).ToString("yyyy-MM-dd HH:mm")},
    };

    var vpcConfig = new AILabVpcConfig(env);
    var SubnetMappings = new InputList<Aws.LB.Inputs.LoadBalancerSubnetMappingArgs>();
    foreach (var subnet in vpcConfig.Subnets)
    {
        var mapping = new Aws.LB.Inputs.LoadBalancerSubnetMappingArgs
        {
            SubnetId = subnet,
        };
        SubnetMappings.Add(mapping);
    }

    var securityGroupConfig = new AILabSecurityGroupConfig(env);
    var loadBalancer = new Awsx.Lb.ApplicationLoadBalancer($"{shortName}-ALB", new()
    {
        SubnetMappings = SubnetMappings,
        Tags = tags,
        SecurityGroups = securityGroupConfig.SecurityGroupIds
    });
    
    return new Dictionary<string, object?>
    {
        ["loadBalancer"] = loadBalancer.GetResourceName(),
    };
});

